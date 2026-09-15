using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Configurations;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Xunit;

namespace Api.Test.Database;

[CollectionDefinition("Postgres diagnostics", DisableParallelization = true)]
public class PostgresDiagnosticsCollection;

[Collection("Postgres diagnostics")]
public class PostgresTokenDiagnosticsTests
{
    private const string Secret = "SENTINEL-TOKEN-AND-PASSWORD";
    private static readonly DateTimeOffset Baseline = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class RecordingLogger : ILogger<PostgresTokenDiagnostics>
    {
        public ConcurrentQueue<Dictionary<string, object?>> Events { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(
            LogLevel level,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Assert.Null(exception);
            Assert.DoesNotContain(Secret, formatter(state, exception));
            var fields = Assert
                .IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state)
                .ToDictionary();
            fields.Add("Event", eventId.Name);
            Events.Enqueue(fields);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("invalid")]
    public void DefaultOffAndInvalidFlag(string? value)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [PostgresTokenDiagnostics.EnabledKey] = value }
            )
            .Build();
        var host = Mock.Of<IHostEnvironment>(host => host.EnvironmentName == "Development");
        if (value == "invalid")
            Assert.Throws<InvalidOperationException>(() =>
                PostgresTokenDiagnostics.IsEnabled(config, host)
            );
        else
            Assert.False(PostgresTokenDiagnostics.IsEnabled(config, host));
    }

    [Fact]
    public async Task ExpiryAndPhysicalSessionRemainIndependentObservationsDuringRefresh()
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        var cancellation = TestContext.Current.CancellationToken;
        async Task Provide(DateTimeOffset expiry) =>
            Assert.Equal(
                Secret,
                await diagnostics.GetPasswordAsync(
                    _ => ValueTask.FromResult(new AccessToken(Secret, expiry)),
                    cancellation
                )
            );
        await Provide(Baseline);
        await Provide(Baseline);
        var pending = new TaskCompletionSource<AccessToken>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var refresh = diagnostics.GetPasswordAsync(_ => new(pending.Task), cancellation);
        using var table = SessionTable();
        table.Rows.Add(1, Baseline.AddSeconds(-1));
        using (var reader = table.CreateDataReader())
            diagnostics.ObservePhysicalSession(123, reader);
        pending.SetResult(new AccessToken(Secret, Baseline.AddHours(1)));
        await refresh;
        await using (var reader = table.CreateDataReader())
            await diagnostics.ObservePhysicalSessionAsync(123, reader, cancellation);

        var providers = log
            .Events.Where(e => Equals(e["Event"], "PostgresTokenProvider"))
            .ToArray();
        Assert.Equal(
            new[] { Baseline, Baseline, Baseline.AddHours(1) },
            providers.Select(e => (DateTimeOffset)e["ExpiresOn"]!)
        );
        Assert.All(
            providers,
            e => Assert.True((DateTimeOffset)e["StartedUtc"]! <= (DateTimeOffset)e["CompletedUtc"]!)
        );
        var sessions = log
            .Events.Where(e => Equals(e["Event"], "PostgresPhysicalSession"))
            .ToArray();
        Assert.Equal(2, sessions.Length);
        Assert.All(
            sessions,
            e =>
            {
                Assert.Equal(123, e["BackendProcessId"]);
                Assert.Equal(Baseline.AddSeconds(-1), e["BackendStartedUtc"]);
                Assert.False(e.ContainsKey("ExpiresOn"));
            }
        );
        Assert.Single(log.Events.Select(e => e["ProcessInstanceId"]).Distinct());
        Assert.Single(log.Events.Select(e => e["DataSourceId"]).Distinct());
        var other = new PostgresTokenDiagnostics(log);
        using (var reader = table.CreateDataReader())
            other.ObservePhysicalSession(123, reader);
        Assert.Equal(2, log.Events.Select(e => e["DataSourceId"]).Distinct().Count());
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("Multiple")]
    [InlineData("Null")]
    [InlineData("BadConstant")]
    [InlineData("NonUtc")]
    [InlineData("Infinite")]
    public async Task InvalidSessionReadPropagatesWithoutSuccess(string failure)
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        using var table = SessionTable();
        if (failure != "Missing")
            table.Rows.Add(
                failure == "BadConstant" ? 0 : 1,
                failure == "Null" ? DBNull.Value
                    : failure == "NonUtc" ? Baseline.ToOffset(TimeSpan.FromHours(1))
                    : failure == "Infinite" ? DateTimeOffset.MaxValue
                    : Baseline
            );
        if (failure == "Multiple")
            table.Rows.Add(1, Baseline);
        using (var reader = table.CreateDataReader())
            Assert.Throws<InvalidOperationException>(() =>
                diagnostics.ObservePhysicalSession(123, reader)
            );
        await using (var reader = table.CreateDataReader())
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                diagnostics.ObservePhysicalSessionAsync(
                    123,
                    reader,
                    TestContext.Current.CancellationToken
                )
            );
        Assert.Empty(log.Events);
    }

    [Fact]
    public async Task ProviderAndReaderFailuresOrCancellationNeverLogSuccess()
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        var failure = new InvalidOperationException(Secret);
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await diagnostics.GetPasswordAsync(
                    _ => ValueTask.FromException<AccessToken>(failure),
                    TestContext.Current.CancellationToken
                )
            )
        );
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await diagnostics.GetPasswordAsync(
                _ => ValueTask.FromResult(new AccessToken(Secret, Baseline)),
                canceled.Token
            )
        );
        using var table = SessionTable();
        table.Rows.Add(1, Baseline);
        await using var reader = table.CreateDataReader();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            diagnostics.ObservePhysicalSessionAsync(123, reader, canceled.Token)
        );
        var broken = new Mock<DbDataReader>();
        broken.Setup(r => r.Read()).Throws(failure);
        broken.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        Assert.Same(
            failure,
            Assert.Throws<InvalidOperationException>(() =>
                diagnostics.ObservePhysicalSession(123, broken.Object)
            )
        );
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                diagnostics.ObservePhysicalSessionAsync(
                    123,
                    broken.Object,
                    TestContext.Current.CancellationToken
                )
            )
        );
        Assert.Empty(log.Events);
    }

    [Fact]
    public void QueryIsFixedBoundedAndUsesSuppliedConnection()
    {
        using var connection = new NpgsqlConnection($"Host=unused;Password={Secret}");
        using var command = PostgresTokenDiagnostics.CreateSessionCommand(connection);
        Assert.Same(connection, command.Connection);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(
            "SELECT 1, backend_start FROM pg_catalog.pg_stat_activity WHERE pid = pg_backend_pid()",
            command.CommandText
        );
        Assert.Equal(5, command.CommandTimeout);
        Assert.Empty(command.Parameters);
    }

    private static DataTable SessionTable()
    {
        var table = new DataTable();
        table.Columns.Add("constant", typeof(int));
        table.Columns.Add("backend_start", typeof(DateTimeOffset));
        return table;
    }
}
