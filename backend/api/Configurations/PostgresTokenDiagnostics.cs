using System.Data.Common;
using Azure.Core;
using Npgsql;

namespace Api.Configurations;

internal sealed class PostgresTokenDiagnostics(ILogger<PostgresTokenDiagnostics> logger)
{
    internal const string EnabledKey = "Database:TokenRenewalDiagnostics:Enabled";
    private static readonly Guid ProcessInstanceId = Guid.NewGuid();
    private readonly Guid dataSourceId = Guid.NewGuid();

    internal static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!configuration.GetValue<bool>(EnabledKey))
            return false;
        if (environment.IsDevelopment())
            return true;

        Console.WriteLine(
            "PostgreSQL token diagnostics Disabled: trusted host environment is not Development."
        );
        return false;
    }

    internal static void ReportUnavailable(string reason) =>
        Console.WriteLine(
            $"PostgreSQL token diagnostics Unavailable: {reason}; token login not tested."
        );

    internal async ValueTask<string> GetPasswordAsync(
        Func<CancellationToken, ValueTask<AccessToken>> getToken,
        CancellationToken cancellationToken
    )
    {
        var startedUtc = DateTimeOffset.UtcNow;
        var token = await getToken(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            new EventId(1, "PostgresTokenProvider"),
            "PostgreSQL provider ProcessInstanceId={ProcessInstanceId} DataSourceId={DataSourceId} "
                + "LocalProcessId={LocalProcessId} StartedUtc={StartedUtc:O} CompletedUtc={CompletedUtc:O} ExpiresOn={ExpiresOn:O}",
            ProcessInstanceId,
            dataSourceId,
            Environment.ProcessId,
            startedUtc,
            DateTimeOffset.UtcNow,
            token.ExpiresOn
        );
        return token.Token;
    }

    internal void InitializePhysicalConnection(NpgsqlConnection connection)
    {
        using var command = CreateSessionCommand(connection);
        using var reader = command.ExecuteReader();
        ObservePhysicalSession(connection.ProcessID, reader);
    }

    internal async Task InitializePhysicalConnectionAsync(NpgsqlConnection connection)
    {
        // Npgsql's physical initializer does not expose the original Open cancellation token.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var command = CreateSessionCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(deadline.Token);
        await ObservePhysicalSessionAsync(connection.ProcessID, reader, deadline.Token);
    }

    internal static NpgsqlCommand CreateSessionCommand(NpgsqlConnection connection) =>
        new(
            "SELECT 1, backend_start FROM pg_catalog.pg_stat_activity WHERE pid = pg_backend_pid()",
            connection
        )
        {
            CommandTimeout = 5,
        };

    internal void ObservePhysicalSession(int backendProcessId, DbDataReader reader)
    {
        if (!reader.Read())
            throw new InvalidOperationException("PostgreSQL diagnostic session row is missing.");
        var backendStartedUtc = ReadSessionRow(reader);
        if (reader.Read())
            throw new InvalidOperationException(
                "PostgreSQL diagnostic returned multiple session rows."
            );
        LogSession(backendProcessId, backendStartedUtc);
    }

    internal async Task ObservePhysicalSessionAsync(
        int backendProcessId,
        DbDataReader reader,
        CancellationToken cancellationToken
    )
    {
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("PostgreSQL diagnostic session row is missing.");
        var backendStartedUtc = ReadSessionRow(reader);
        if (await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException(
                "PostgreSQL diagnostic returned multiple session rows."
            );
        cancellationToken.ThrowIfCancellationRequested();
        LogSession(backendProcessId, backendStartedUtc);
    }

    private static DateTimeOffset ReadSessionRow(DbDataReader reader)
    {
        if (
            reader.FieldCount != 2
            || reader.IsDBNull(0)
            || reader.IsDBNull(1)
            || reader.GetInt32(0) != 1
        )
            throw new InvalidOperationException(
                "PostgreSQL diagnostic session metadata is invalid."
            );
        var startedUtc = reader.GetFieldValue<DateTimeOffset>(1);
        if (
            startedUtc.Offset != TimeSpan.Zero
            || startedUtc == DateTimeOffset.MinValue
            || startedUtc == DateTimeOffset.MaxValue
        )
            throw new InvalidOperationException(
                "PostgreSQL diagnostic backend start must be a finite UTC timestamp."
            );
        return startedUtc;
    }

    // No provider snapshot is attached: a concurrent refresh cannot identify the token used to log in.
    private void LogSession(int backendProcessId, DateTimeOffset backendStartedUtc) =>
        logger.LogInformation(
            new EventId(2, "PostgresPhysicalSession"),
            "PostgreSQL physical session read succeeded ProcessInstanceId={ProcessInstanceId} DataSourceId={DataSourceId} "
                + "LocalProcessId={LocalProcessId} BackendProcessId={BackendProcessId} BackendStartedUtc={BackendStartedUtc:O} ObservedUtc={ObservedUtc:O}",
            ProcessInstanceId,
            dataSourceId,
            Environment.ProcessId,
            backendProcessId,
            backendStartedUtc,
            DateTimeOffset.UtcNow
        );
}
