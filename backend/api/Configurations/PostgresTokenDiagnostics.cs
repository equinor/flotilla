using System.Data.Common;
using System.Text.Json;
using Azure.Core;
using Npgsql;

namespace Api.Configurations;

internal sealed class PostgresTokenDiagnostics(
    ILogger<PostgresTokenDiagnostics> logger,
    TimeProvider? clock = null
)
{
    internal const string EnabledKey = "Database:TokenRenewalDiagnostics:Enabled";
    private const string SessionQuery =
        "SELECT 1, backend_start FROM pg_catalog.pg_stat_activity WHERE pid = pg_backend_pid()";
    private static readonly Guid ProcessInstanceId = Guid.NewGuid();
    private readonly Guid dataSourceId = Guid.NewGuid();
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private readonly object gate = new();
    private long sequence;
    private long? lastCompletedSequence;
    private DateTimeOffset? lastExpiresOn;
    private int pendingCallbacks;

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
        long callbackSequence;
        DateTimeOffset startedUtc;
        lock (gate)
        {
            callbackSequence = ++sequence;
            startedUtc = time.GetUtcNow();
            pendingCallbacks++;
            Write(new("ProviderStarted", startedUtc, callbackSequence, startedUtc));
        }

        AccessToken token;
        try
        {
            token = await getToken(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception exception)
        {
            lock (gate)
            {
                pendingCallbacks--;
                Write(
                    new(
                        exception is OperationCanceledException
                            ? "ProviderCanceled"
                            : "ProviderFailed",
                        time.GetUtcNow(),
                        callbackSequence,
                        startedUtc
                    ),
                    LogLevel.Warning
                );
            }
            throw;
        }

        lock (gate)
        {
            pendingCallbacks--;
            var previousExpiry = lastExpiresOn;
            lastExpiresOn = token.ExpiresOn;
            lastCompletedSequence = callbackSequence;
            Write(
                new(
                    "ProviderCompleted",
                    time.GetUtcNow(),
                    callbackSequence,
                    startedUtc,
                    token.ExpiresOn,
                    previousExpiry,
                    previousExpiry.HasValue ? token.ExpiresOn > previousExpiry : null
                )
            );
        }
        return token.Token;
    }

    internal void InitializePhysicalConnection(NpgsqlConnection connection)
    {
        using var command = CreateSessionCommand(connection);
        ObservePhysicalRead(
            connection.ProcessID,
            () =>
            {
                using var reader = command.ExecuteReader();
                return ReadBackendStart(reader);
            }
        );
    }

    internal async Task InitializePhysicalConnectionAsync(NpgsqlConnection connection)
    {
        await using var command = CreateSessionCommand(connection);
        await ObservePhysicalReadAsync(
            connection.ProcessID,
            async cancellationToken =>
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                return await ReadBackendStartAsync(reader, cancellationToken);
            }
        );
    }

    internal static NpgsqlCommand CreateSessionCommand(NpgsqlConnection connection) =>
        new(SessionQuery, connection) { CommandTimeout = 5 };

    // Npgsql's initializer runs after authentication, but exposes neither the password used
    // nor the caller's cancellation token. These snapshots must never attribute a token to a login.
    internal void ObservePhysicalRead(int backendProcessId, Func<DateTimeOffset> read)
    {
        var physicalConnectionId = ObserveAuthentication(backendProcessId);
        DateTimeOffset backendStartedUtc;
        try
        {
            backendStartedUtc = read();
            ValidateBackendStart(backendStartedUtc);
        }
        catch (Exception exception)
        {
            ObserveReadFailure(physicalConnectionId, backendProcessId, exception);
            throw;
        }
        ObserveReadSuccess(physicalConnectionId, backendProcessId, backendStartedUtc);
    }

    internal async Task ObservePhysicalReadAsync(
        int backendProcessId,
        Func<CancellationToken, ValueTask<DateTimeOffset>> read,
        CancellationToken cancellationToken = default
    )
    {
        var physicalConnectionId = ObserveAuthentication(backendProcessId);

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5), time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            deadline.Token
        );
        DateTimeOffset backendStartedUtc;
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            backendStartedUtc = await read(linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            ValidateBackendStart(backendStartedUtc);
        }
        catch (Exception exception)
        {
            ObserveReadFailure(physicalConnectionId, backendProcessId, exception);
            throw;
        }

        ObserveReadSuccess(physicalConnectionId, backendProcessId, backendStartedUtc);
    }

    private Guid ObserveAuthentication(int backendProcessId)
    {
        var physicalConnectionId = Guid.NewGuid();
        lock (gate)
            Write(
                new(
                    "PhysicalAuthenticated",
                    time.GetUtcNow(),
                    PhysicalConnectionId: physicalConnectionId,
                    BackendProcessId: backendProcessId
                )
            );
        return physicalConnectionId;
    }

    private void ObserveReadSuccess(
        Guid physicalConnectionId,
        int backendProcessId,
        DateTimeOffset backendStartedUtc
    )
    {
        lock (gate)
            Write(
                new(
                    "PhysicalReadSucceeded",
                    time.GetUtcNow(),
                    PhysicalConnectionId: physicalConnectionId,
                    BackendProcessId: backendProcessId,
                    BackendStartedUtc: backendStartedUtc
                )
            );
    }

    private void ObserveReadFailure(
        Guid physicalConnectionId,
        int backendProcessId,
        Exception exception
    )
    {
        lock (gate)
            Write(
                new(
                    exception is OperationCanceledException
                        ? "PhysicalReadCanceled"
                        : "PhysicalReadFailed",
                    time.GetUtcNow(),
                    PhysicalConnectionId: physicalConnectionId,
                    BackendProcessId: backendProcessId
                ),
                LogLevel.Warning
            );
    }

    internal static DateTimeOffset ReadBackendStart(DbDataReader reader)
    {
        if (!reader.Read())
            throw new InvalidOperationException("PostgreSQL diagnostic session row is missing.");
        var backendStartedUtc = ReadSessionRow(reader);
        if (reader.Read())
            throw new InvalidOperationException(
                "PostgreSQL diagnostic returned multiple session rows."
            );
        return backendStartedUtc;
    }

    internal static async Task<DateTimeOffset> ReadBackendStartAsync(
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
        return backendStartedUtc;
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
        var backendStartedUtc = reader.GetFieldValue<DateTimeOffset>(1);
        ValidateBackendStart(backendStartedUtc);
        return backendStartedUtc;
    }

    private static void ValidateBackendStart(DateTimeOffset backendStartedUtc)
    {
        if (
            backendStartedUtc.Offset != TimeSpan.Zero
            || backendStartedUtc == DateTimeOffset.MinValue
            || backendStartedUtc == DateTimeOffset.MaxValue
        )
            throw new InvalidOperationException(
                "PostgreSQL diagnostic backend start must be a finite UTC timestamp."
            );
    }

    private void Write(Observation observation, LogLevel level = LogLevel.Information) =>
        logger.Log(
            level,
            "PostgreSQL token diagnostics {Observation}",
            JsonSerializer.Serialize(
                observation with
                {
                    ProcessInstanceId = ProcessInstanceId,
                    LocalProcessId = Environment.ProcessId,
                    DataSourceId = dataSourceId,
                    LastCompletedProviderSequence = lastCompletedSequence,
                    LastObservedProviderExpiresOn = lastExpiresOn,
                    PendingCallbacks = pendingCallbacks,
                }
            )
        );

    private sealed record Observation(
        string Event,
        DateTimeOffset ObservedUtc,
        long? ProviderSequence = null,
        DateTimeOffset? ProviderStartedUtc = null,
        DateTimeOffset? ProviderExpiresOn = null,
        DateTimeOffset? PreviousProviderExpiresOn = null,
        bool? ExpiryAdvanced = null,
        Guid? PhysicalConnectionId = null,
        int? BackendProcessId = null,
        DateTimeOffset? BackendStartedUtc = null
    )
    {
        public Guid ProcessInstanceId { get; init; }
        public int LocalProcessId { get; init; }
        public Guid DataSourceId { get; init; }
        public long? LastCompletedProviderSequence { get; init; }
        public DateTimeOffset? LastObservedProviderExpiresOn { get; init; }
        public int PendingCallbacks { get; init; }
        public string TokenAttribution => "Unknown";
    }
}
