using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    private const string SecretToken = "SENTINEL-TOKEN-NEVER-LOG";
    private const string SecretConnection = "Host=secret;Password=SENTINEL-PASSWORD";
    private static readonly DateTimeOffset Baseline = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = Baseline;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class RecordingLogger : ILogger<PostgresTokenDiagnostics>
    {
        public ConcurrentQueue<JsonElement> Events { get; } = new();

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
            var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            var json = Assert.IsType<string>(
                fields.Single(field => field.Key == "Observation").Value
            );
            Assert.DoesNotContain(SecretToken, json);
            Assert.DoesNotContain(SecretConnection, json);
            using var document = JsonDocument.Parse(json);
            Events.Enqueue(document.RootElement.Clone());
        }
    }

    private static ValueTask<string> Provide(
        PostgresTokenDiagnostics diagnostics,
        DateTimeOffset expiry,
        string token = SecretToken
    ) =>
        diagnostics.GetPasswordAsync(
            _ => ValueTask.FromResult(new AccessToken(token, expiry)),
            TestContext.Current.CancellationToken
        );

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Test")]
    [InlineData("Local")]
    [InlineData("IntegrationTest")]
    public void MissingFlagDefaultsOff(string environment)
    {
        Assert.False(
            PostgresTokenDiagnostics.IsEnabled(
                new ConfigurationBuilder().Build(),
                Mock.Of<IHostEnvironment>(host => host.EnvironmentName == environment)
            )
        );
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("dEvElOpMeNt", true)]
    [InlineData("Staging", false)]
    [InlineData("sTaGiNg", false)]
    [InlineData("Production", false)]
    [InlineData("pRoDuCtIoN", false)]
    [InlineData("Test", false)]
    [InlineData("Local", false)]
    [InlineData("IntegrationTest", false)]
    public void OnlyTrustedDevelopmentEnablesDiagnostics(string environment, bool expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [PostgresTokenDiagnostics.EnabledKey] = "true",
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["environment"] = "Development",
                }
            )
            .Build();
        var original = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            Assert.Equal(
                expected,
                PostgresTokenDiagnostics.IsEnabled(
                    config,
                    Mock.Of<IHostEnvironment>(host => host.EnvironmentName == environment)
                )
            );
        }
        finally
        {
            Console.SetOut(original);
        }
        if (!expected)
            Assert.Contains(
                "Disabled: trusted host environment is not Development",
                output.ToString()
            );
    }

    [Fact]
    public void InvalidFlagIsNotSilentlyDisabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [PostgresTokenDiagnostics.EnabledKey] = "invalid",
                }
            )
            .Build();
        Assert.Throws<InvalidOperationException>(() =>
            PostgresTokenDiagnostics.IsEnabled(
                config,
                Mock.Of<IHostEnvironment>(host => host.EnvironmentName == "Development")
            )
        );
    }

    [Fact]
    public async Task ProviderExpiryObservationsDoNotClaimCacheHitsOrTokenRenewal()
    {
        var log = new RecordingLogger();
        var clock = new Clock();
        var diagnostics = new PostgresTokenDiagnostics(log, clock);
        var expiry = Baseline.AddHours(1);
        Assert.Equal(SecretToken, await Provide(diagnostics, expiry));
        await Provide(diagnostics, expiry);
        await Provide(diagnostics, expiry, "different-token-same-expiry");
        clock.UtcNow = Baseline.AddMinutes(55);
        await Provide(diagnostics, Baseline.AddHours(2));

        var completed = log
            .Events.Where(e => e.GetProperty("Event").GetString() == "ProviderCompleted")
            .ToArray();
        Assert.Equal(4, completed.Length);
        Assert.Equal(JsonValueKind.Null, completed[0].GetProperty("ExpiryAdvanced").ValueKind);
        Assert.False(completed[1].GetProperty("ExpiryAdvanced").GetBoolean());
        Assert.False(completed[2].GetProperty("ExpiryAdvanced").GetBoolean());
        Assert.True(completed[3].GetProperty("ExpiryAdvanced").GetBoolean());
        Assert.Equal(expiry, completed[0].GetProperty("ProviderExpiresOn").GetDateTimeOffset());
        Assert.Equal(Baseline, completed[0].GetProperty("ProviderStartedUtc").GetDateTimeOffset());
        Assert.Equal(clock.UtcNow, completed[3].GetProperty("ObservedUtc").GetDateTimeOffset());
        Assert.Equal(
            new long[] { 1, 2, 3, 4 },
            completed.Select(e => e.GetProperty("ProviderSequence").GetInt64())
        );
        Assert.All(
            log.Events,
            e => Assert.Equal("Unknown", e.GetProperty("TokenAttribution").GetString())
        );
    }

    [Fact]
    public async Task RefreshDuringPhysicalReadNeverAttributesTokenToConnection()
    {
        var log = new RecordingLogger();
        var clock = new Clock();
        var diagnostics = new PostgresTokenDiagnostics(log, clock);
        await Provide(diagnostics, Baseline.AddHours(1));
        var readResult = new TaskCompletionSource<DateTimeOffset>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var reading = diagnostics.ObservePhysicalReadAsync(
            123,
            ct => new(readResult.Task.WaitAsync(ct)),
            TestContext.Current.CancellationToken
        );

        clock.UtcNow = Baseline.AddHours(1).AddMinutes(1);
        await Provide(diagnostics, Baseline.AddHours(2));
        readResult.SetResult(Baseline);
        await reading;

        var opened = log.Events.Single(e =>
            e.GetProperty("Event").GetString() == "PhysicalAuthenticated"
        );
        var succeeded = log.Events.Single(e =>
            e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
        Assert.Equal(Baseline, opened.GetProperty("ObservedUtc").GetDateTimeOffset());
        Assert.Equal(clock.UtcNow, succeeded.GetProperty("ObservedUtc").GetDateTimeOffset());
        Assert.Equal(1, opened.GetProperty("LastCompletedProviderSequence").GetInt64());
        Assert.Equal(2, succeeded.GetProperty("LastCompletedProviderSequence").GetInt64());
        Assert.Equal(
            opened.GetProperty("PhysicalConnectionId").GetGuid(),
            succeeded.GetProperty("PhysicalConnectionId").GetGuid()
        );
        Assert.Equal(123, succeeded.GetProperty("BackendProcessId").GetInt32());
        Assert.Equal(Baseline, succeeded.GetProperty("BackendStartedUtc").GetDateTimeOffset());
        Assert.All(
            log.Events,
            e => Assert.Equal("Unknown", e.GetProperty("TokenAttribution").GetString())
        );
    }

    [Fact]
    public async Task OpenWhileRefreshPendingAndOutOfOrderCallbacksStayObservationsOnly()
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log, new Clock());
        var first = new TaskCompletionSource<AccessToken>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var pending = diagnostics.GetPasswordAsync(
            _ => new(first.Task),
            TestContext.Current.CancellationToken
        );
        await Provide(diagnostics, Baseline.AddHours(2));
        diagnostics.ObservePhysicalRead(123, () => Baseline);
        first.SetResult(new AccessToken(SecretToken, Baseline.AddHours(1)));
        await pending;

        var read = log.Events.Single(e =>
            e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
        Assert.Equal(1, read.GetProperty("PendingCallbacks").GetInt32());
        Assert.Equal(2, read.GetProperty("LastCompletedProviderSequence").GetInt64());
        var last = log.Events.Last();
        Assert.Equal(1, last.GetProperty("ProviderSequence").GetInt64());
        Assert.False(last.GetProperty("ExpiryAdvanced").GetBoolean());
        Assert.Equal(0, last.GetProperty("PendingCallbacks").GetInt32());
        Assert.All(
            log.Events,
            e => Assert.Equal("Unknown", e.GetProperty("TokenAttribution").GetString())
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderFailureOrCancellationCannotProduceCompletion(bool canceled)
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        Exception failure = canceled
            ? new OperationCanceledException(SecretToken)
            : new InvalidOperationException(SecretConnection);
        var thrown = await Assert.ThrowsAsync(
            failure.GetType(),
            async () =>
                await diagnostics.GetPasswordAsync(
                    _ => ValueTask.FromException<AccessToken>(failure),
                    TestContext.Current.CancellationToken
                )
        );
        Assert.Same(failure, thrown);
        Assert.Equal(
            canceled ? "ProviderCanceled" : "ProviderFailed",
            log.Events.Last().GetProperty("Event").GetString()
        );
        Assert.DoesNotContain(
            log.Events,
            e => e.GetProperty("Event").GetString() == "ProviderCompleted"
        );
        Assert.Equal(0, log.Events.Last().GetProperty("PendingCallbacks").GetInt32());
    }

    [Fact]
    public async Task CanceledProviderReturningTokenCannotProduceCompletion()
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await diagnostics.GetPasswordAsync(
                _ => ValueTask.FromResult(new AccessToken(SecretToken, Baseline.AddHours(1))),
                cancellation.Token
            )
        );
        Assert.DoesNotContain(
            log.Events,
            e => e.GetProperty("Event").GetString() == "ProviderCompleted"
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadFailuresPropagateWithoutFalseSuccessOrSensitiveLogs(bool async)
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        var failure = new InvalidOperationException(SecretToken + SecretConnection);
        if (async)
            Assert.Same(
                failure,
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    diagnostics.ObservePhysicalReadAsync(
                        123,
                        _ => ValueTask.FromException<DateTimeOffset>(failure),
                        TestContext.Current.CancellationToken
                    )
                )
            );
        else
            Assert.Same(
                failure,
                Assert.Throws<InvalidOperationException>(() =>
                    diagnostics.ObservePhysicalRead(123, () => throw failure)
                )
            );
        Assert.Equal("PhysicalReadFailed", log.Events.Last().GetProperty("Event").GetString());
        Assert.DoesNotContain(
            log.Events,
            e => e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
    }

    [Theory]
    [InlineData("MissingRow")]
    [InlineData("MultipleRows")]
    [InlineData("NullConstant")]
    [InlineData("WrongConstant")]
    [InlineData("NullTimestamp")]
    [InlineData("NonUtcTimestamp")]
    [InlineData("InfiniteTimestamp")]
    public async Task UnexpectedSessionMetadataCannotProduceSuccess(string invalid)
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        using var table = SessionTable();
        if (invalid != "MissingRow")
            table.Rows.Add(
                invalid == "NullConstant" ? DBNull.Value
                    : invalid == "WrongConstant" ? 0
                    : 1,
                invalid == "NullTimestamp" ? DBNull.Value
                    : invalid == "NonUtcTimestamp" ? Baseline.ToOffset(TimeSpan.FromHours(1))
                    : invalid == "InfiniteTimestamp" ? DateTimeOffset.MaxValue
                    : Baseline
            );
        if (invalid == "MultipleRows")
            table.Rows.Add(1, Baseline);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            diagnostics.ObservePhysicalReadAsync(
                123,
                async cancellationToken =>
                {
                    await using var reader = table.CreateDataReader();
                    return await PostgresTokenDiagnostics.ReadBackendStartAsync(
                        reader,
                        cancellationToken
                    );
                },
                TestContext.Current.CancellationToken
            )
        );
        Assert.Throws<InvalidOperationException>(() =>
            diagnostics.ObservePhysicalRead(
                123,
                () =>
                {
                    using var reader = table.CreateDataReader();
                    return PostgresTokenDiagnostics.ReadBackendStart(reader);
                }
            )
        );
        Assert.DoesNotContain(
            log.Events,
            e => e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
    }

    private static DataTable SessionTable()
    {
        var table = new DataTable();
        table.Columns.Add("constant", typeof(int));
        table.Columns.Add("backend_start", typeof(DateTimeOffset));
        return table;
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ServerBackendStartPreservesExpiryBoundaryDespiteDelayedInitializer(
        int secondsAfterExpiry
    )
    {
        var log = new RecordingLogger();
        var clock = new Clock();
        var diagnostics = new PostgresTokenDiagnostics(log, clock);
        var expiry = Baseline.AddHours(1);
        await Provide(diagnostics, expiry);
        clock.UtcNow = expiry.AddMinutes(1);
        await Provide(diagnostics, expiry.AddHours(1));
        var backendStartedUtc = expiry.AddSeconds(secondsAfterExpiry);
        using var table = SessionTable();
        table.Rows.Add(1, backendStartedUtc);
        diagnostics.ObservePhysicalRead(
            123,
            () =>
            {
                using var reader = table.CreateDataReader();
                return PostgresTokenDiagnostics.ReadBackendStart(reader);
            }
        );
        await diagnostics.ObservePhysicalReadAsync(
            124,
            async cancellationToken =>
            {
                await using var reader = table.CreateDataReader();
                return await PostgresTokenDiagnostics.ReadBackendStartAsync(
                    reader,
                    cancellationToken
                );
            },
            TestContext.Current.CancellationToken
        );
        var succeeded = log.Events.Where(e =>
            e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
        Assert.All(
            succeeded,
            e =>
            {
                Assert.True(e.GetProperty("ObservedUtc").GetDateTimeOffset() > expiry);
                Assert.Equal(
                    secondsAfterExpiry > 0,
                    e.GetProperty("BackendStartedUtc").GetDateTimeOffset() > expiry
                );
                Assert.Equal("Unknown", e.GetProperty("TokenAttribution").GetString());
            }
        );
    }

    [Fact]
    public async Task ReadHonorsFiveSecondIndependentDeadline()
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        var elapsed = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            diagnostics
                .ObservePhysicalReadAsync(
                    123,
                    async cancellationToken =>
                    {
                        Assert.True(cancellationToken.CanBeCanceled);
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                        return Baseline;
                    },
                    TestContext.Current.CancellationToken
                )
                .WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken)
        );
        Assert.True(elapsed.Elapsed >= TimeSpan.FromSeconds(4.5));
        Assert.Equal("PhysicalReadCanceled", log.Events.Last().GetProperty("Event").GetString());
        Assert.DoesNotContain(
            log.Events,
            e => e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
    }

    [Fact]
    public void ProbeUsesOnlyFixedOwnSessionQueryOnSuppliedConnectionWithFiveSecondTimeout()
    {
        using var connection = new NpgsqlConnection(SecretConnection);
        using var command = PostgresTokenDiagnostics.CreateSessionCommand(connection);
        Assert.Same(connection, command.Connection);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(
            "SELECT 1, backend_start FROM pg_catalog.pg_stat_activity WHERE pid = pg_backend_pid()",
            command.CommandText
        );
        Assert.Equal(5, command.CommandTimeout);
        Assert.Empty(command.Parameters);
        Assert.Null(command.Transaction);
    }

    [Fact]
    public async Task ReadCanceledBeforeCompletionCannotProduceSuccess()
    {
        var log = new RecordingLogger();
        var diagnostics = new PostgresTokenDiagnostics(log);
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            diagnostics.ObservePhysicalReadAsync(
                123,
                _ =>
                {
                    cancellation.Cancel();
                    return ValueTask.FromResult(Baseline);
                },
                cancellation.Token
            )
        );
        Assert.DoesNotContain(
            log.Events,
            e => e.GetProperty("Event").GetString() == "PhysicalReadSucceeded"
        );
    }

    [Fact]
    public async Task ProcessIsStableWhileDataSourcesAndPhysicalObservationsAreDistinct()
    {
        var log = new RecordingLogger();
        var first = new PostgresTokenDiagnostics(log);
        var second = new PostgresTokenDiagnostics(log);
        await Provide(first, Baseline.AddHours(1));
        first.ObservePhysicalRead(123, () => Baseline);
        await first.ObservePhysicalReadAsync(
            123,
            _ => ValueTask.FromResult(Baseline),
            TestContext.Current.CancellationToken
        );
        second.ObservePhysicalRead(123, () => Baseline);
        Assert.Single(
            log.Events.Select(e => e.GetProperty("ProcessInstanceId").GetGuid()).Distinct()
        );
        Assert.All(
            log.Events,
            e => Assert.Equal(Environment.ProcessId, e.GetProperty("LocalProcessId").GetInt32())
        );
        Assert.Equal(
            2,
            log.Events.Select(e => e.GetProperty("DataSourceId").GetGuid()).Distinct().Count()
        );
        var reads = log
            .Events.Where(e => e.GetProperty("Event").GetString() == "PhysicalReadSucceeded")
            .ToArray();
        Assert.Equal(
            3,
            reads.Select(e => e.GetProperty("PhysicalConnectionId").GetGuid()).Distinct().Count()
        );
        Assert.Equal(
            JsonValueKind.Null,
            reads.Last().GetProperty("LastCompletedProviderSequence").ValueKind
        );
        Assert.All(
            reads,
            e => Assert.Equal("Unknown", e.GetProperty("TokenAttribution").GetString())
        );
    }
}
