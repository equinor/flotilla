using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Api.Database.Context;
using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Xunit;

#pragma warning disable EF1001

namespace Api.Test.Database
{
    public class DesignTimeContextFactoryTests
    {
        private const string LegacyKey = "Database:postgresConnectionString";
        private const string LegacyConnection =
            "Host=localhost;Database=legacy;Username=local;Password=synthetic-password";
        private const string Tenant = "11111111-1111-1111-1111-111111111111";

        private sealed class TrackingConfiguration : ConfigurationProvider, IConfigurationSource
        {
            public HashSet<string> Reads { get; } = new(StringComparer.OrdinalIgnoreCase);

            public TrackingConfiguration(string? mode = "AzureCli")
            {
                Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Migrations:AuthenticationMode"] = mode,
                    ["Migrations:Postgres:Host"] = "migration.postgres.database.azure.com",
                    ["Migrations:Postgres:Database"] = "migration-db",
                    ["Migrations:Postgres:Username"] = "migration-role",
                    ["AZURE_TENANT_ID"] = Tenant,
                    [LegacyKey] = LegacyConnection,
                    ["Database:PostgreSqlConnectionString"] = LegacyConnection,
                    ["Database:AuthenticationMode"] = "arbitrary-runtime-policy",
                    ["Database:AuthMethods:0"] = "ManagedIdentity",
                    ["KeyVault:VaultUri"] = "https://synthetic.vault.azure.net/",
                    ["AzureAd:TenantId"] = "must-not-be-used",
                    ["AzureAd:AllowUsingClientSecret"] = "true",
                };
            }

            public override bool TryGet(string key, out string? value)
            {
                Reads.Add(key);
                return base.TryGet(key, out value);
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

            public IConfigurationRoot Configuration => new ConfigurationBuilder().Add(this).Build();
        }

        private sealed class StubCredential(Exception? failure = null) : TokenCredential
        {
            public int Calls { get; private set; }
            public CancellationToken LastCancellation { get; private set; }
            public AccessToken? Result { get; init; }
            public bool WaitForCancellation { get; init; }

            public override AccessToken GetToken(
                TokenRequestContext requestContext,
                CancellationToken cancellationToken
            )
            {
                Calls++;
                Assert.Equal(
                    ["https://ossrdbms-aad.database.windows.net/.default"],
                    requestContext.Scopes
                );
                Assert.True(cancellationToken.CanBeCanceled);
                LastCancellation = cancellationToken;
                cancellationToken.ThrowIfCancellationRequested();
                if (failure is not null)
                    throw failure;
                return Result
                    ?? new AccessToken(
                        $"synthetic-token-{Calls}",
                        DateTimeOffset.UtcNow.AddHours(1)
                    );
            }

            public override async ValueTask<AccessToken> GetTokenAsync(
                TokenRequestContext requestContext,
                CancellationToken cancellationToken
            )
            {
                if (WaitForCancellation)
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                return GetToken(requestContext, cancellationToken);
            }
        }

        private static NpgsqlOptionsExtension Options(FlotillaDbContext context) =>
            Assert.IsType<NpgsqlOptionsExtension>(
                context.GetService<IDbContextOptions>().FindExtension<NpgsqlOptionsExtension>()
            );

        [Theory]
        [InlineData(null)]
        [InlineData("Legacy")]
        [InlineData("lEgAcY")]
        public void LegacyUsesExistingDirectConnectionWithoutCredentialsOrSecrets(string? mode)
        {
            var configuration = new TrackingConfiguration(mode);
            using var context = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                _ => throw new Exception("Unexpected credential"),
                _ => throw new Exception("Unexpected secret")
            );

            Assert.Equal(LegacyConnection, Options(context).ConnectionString);
            Assert.Null(Options(context).DataSource);
            Assert.Equal(
                QuerySplittingBehavior.SingleQuery,
                Options(context).QuerySplittingBehavior
            );
            Assert.Contains(LegacyKey, configuration.Reads);
            Assert.DoesNotContain("KeyVault:VaultUri", configuration.Reads);
            Assert.DoesNotContain("AZURE_TENANT_ID", configuration.Reads);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void LegacyMissingOrEmptyConnectionUsesExistingVault(string? connectionString)
        {
            var configuration = new TrackingConfiguration(null);
            configuration.Set(LegacyKey, connectionString);
            int secretCalls = 0;
            using var context = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                _ => throw new Exception("Unexpected CLI credential"),
                uri =>
                {
                    Assert.Equal(new Uri("https://synthetic.vault.azure.net/"), uri);
                    secretCalls++;
                    return LegacyConnection;
                }
            );

            Assert.Equal(1, secretCalls);
            Assert.Equal(LegacyConnection, Options(context).ConnectionString);
            Assert.Contains("KeyVault:VaultUri", configuration.Reads);
        }

        [Fact]
        public void LegacyMissingVaultAndSecretFailuresStillPropagate()
        {
            var configuration = new TrackingConfiguration(null);
            configuration.Set(LegacyKey, null);
            var failure = new InvalidOperationException("Synthetic secret failure");
            Assert.Same(
                failure,
                Assert.Throws<InvalidOperationException>(() =>
                    DesignTimeContextFactory.CreateDbContext(
                        configuration.Configuration,
                        readSecret: _ => throw failure
                    )
                )
            );
            configuration.Set("KeyVault:VaultUri", null);
            Assert.Throws<KeyNotFoundException>(() =>
                DesignTimeContextFactory.CreateDbContext(configuration.Configuration)
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("AzureCli ")]
        [InlineData("azure_cli")]
        [InlineData("ManagedIdentity")]
        public void InvalidModeFailsBeforeAnyAuthenticationConfiguration(string mode)
        {
            var configuration = new TrackingConfiguration(mode);
            Assert.Throws<InvalidOperationException>(() =>
                DesignTimeContextFactory.CreateDbContext(configuration.Configuration)
            );
            Assert.Equal(["Migrations:AuthenticationMode"], configuration.Reads);
        }

        [Theory]
        [InlineData("Migrations:Postgres:Host", null)]
        [InlineData("Migrations:Postgres:Host", "")]
        [InlineData("Migrations:Postgres:Host", " host.example")]
        [InlineData("Migrations:Postgres:Host", "https://host.example")]
        [InlineData("Migrations:Postgres:Host", "host.example:5432")]
        [InlineData("Migrations:Postgres:Host", "one.example,two.example")]
        [InlineData("Migrations:Postgres:Host", "/tmp/socket")]
        [InlineData("Migrations:Postgres:Database", null)]
        [InlineData("Migrations:Postgres:Database", " ")]
        [InlineData("Migrations:Postgres:Database", "db\nname")]
        [InlineData("Migrations:Postgres:Username", null)]
        [InlineData("Migrations:Postgres:Username", "role ")]
        [InlineData("AZURE_TENANT_ID", null)]
        [InlineData("AZURE_TENANT_ID", "common")]
        [InlineData("AZURE_TENANT_ID", "00000000-0000-0000-0000-000000000000")]
        public void InvalidAzureCliSettingsFailWithoutCredentialOrLegacyReads(
            string key,
            string? value
        )
        {
            var configuration = new TrackingConfiguration();
            configuration.Set(key, value);
            var error = Assert.Throws<InvalidOperationException>(() =>
                DesignTimeContextFactory.CreateDbContext(
                    configuration.Configuration,
                    _ => throw new Exception("Unexpected credential creation"),
                    _ => throw new Exception("Unexpected secret")
                )
            );
            Assert.Contains(key, error.Message);
            AssertNoLegacyReads(configuration);
        }

        [Theory]
        [InlineData("AzureCli")]
        [InlineData("aZuReClI")]
        public void AzureCliUsesDedicatedSettingsAndDoesNotAcquireTokenForModel(string mode)
        {
            var configuration = new TrackingConfiguration(mode);
            configuration.Set("Migrations:Postgres:Database", "db;Password=not-a-password");
            configuration.Set("Migrations:Postgres:Username", "role;Host=not-a-host");
            var credential = new StubCredential();
            using var context = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                options =>
                {
                    Assert.Equal(Tenant, options.TenantId);
                    Assert.Equal(TimeSpan.FromSeconds(30), options.ProcessTimeout);
                    return credential;
                },
                _ => throw new Exception("Unexpected secret")
            );
            var source = Assert.IsAssignableFrom<NpgsqlDataSource>(Options(context).DataSource);
            var connection = new NpgsqlConnectionStringBuilder(source.ConnectionString);
            Assert.Equal("migration.postgres.database.azure.com", connection.Host);
            Assert.Equal("db;Password=not-a-password", connection.Database);
            Assert.Equal("role;Host=not-a-host", connection.Username);
            Assert.Equal(SslMode.VerifyFull, connection.SslMode);
            Assert.True(string.IsNullOrEmpty(connection.Password));
            Assert.Equal(
                QuerySplittingBehavior.SingleQuery,
                Options(context).QuerySplittingBehavior
            );
            Assert.NotEmpty(context.Model.GetEntityTypes());
            Assert.Equal(0, credential.Calls);
            AssertNoLegacyReads(configuration);
        }

        [Fact]
        public async Task TokenCallbacksAcquireAgainAndPropagateCancellation()
        {
            var credential = new StubCredential();
            Assert.Equal("synthetic-token-1", DesignTimeContextFactory.GetPassword(credential));
            using var cancellation = new CancellationTokenSource();
            Assert.Equal(
                "synthetic-token-2",
                await DesignTimeContextFactory.GetPasswordAsync(credential, cancellation.Token)
            );
            Assert.Equal(2, credential.Calls);
            cancellation.Cancel();
            // A completed callback must dispose its linked cancellation registration.
            Assert.False(credential.LastCancellation.IsCancellationRequested);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await DesignTimeContextFactory.GetPasswordAsync(credential, cancellation.Token)
            );
        }

        [Fact]
        public async Task ProviderClonesInvokeFreshSyncAndAsyncCallbacksWithoutConnecting()
        {
            var configuration = new TrackingConfiguration();
            var credential = new StubCredential();
            using var context = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                _ => credential
            );
            var source = Assert.IsAssignableFrom<NpgsqlDataSource>(Options(context).DataSource);
            using var connection = source.CreateConnection();
            using var syncClone = connection.CloneWith(connection.ConnectionString);
            Assert.Equal(1, credential.Calls);
            using var asyncClone = await connection.CloneWithAsync(
                connection.ConnectionString,
                TestContext.Current.CancellationToken
            );
            Assert.Equal(2, credential.Calls);
            Assert.Equal(
                "synthetic-token-1",
                new NpgsqlConnectionStringBuilder(syncClone.ConnectionString).Password
            );
            Assert.Equal(
                "synthetic-token-2",
                new NpgsqlConnectionStringBuilder(asyncClone.ConnectionString).Password
            );
            Assert.True(
                string.IsNullOrEmpty(
                    new NpgsqlConnectionStringBuilder(source.ConnectionString).Password
                )
            );
            AssertNoLegacyReads(configuration);
        }

        [Fact]
        public async Task ProviderFailuresNeverReadLegacyConfigurationOrExposeCliOutput()
        {
            var configuration = new TrackingConfiguration();
            var credential = new StubCredential(
                new AuthenticationFailedException("sentinel-cli-output")
            );
            using var context = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                _ => credential
            );
            var source = Assert.IsAssignableFrom<NpgsqlDataSource>(Options(context).DataSource);
            using var connection = source.CreateConnection();
            var syncError = Assert.Throws<NpgsqlException>(() =>
                connection.CloneWith(connection.ConnectionString)
            );
            var asyncError = await Assert.ThrowsAsync<NpgsqlException>(async () =>
                await connection.CloneWithAsync(
                    connection.ConnectionString,
                    TestContext.Current.CancellationToken
                )
            );
            foreach (var error in new[] { syncError, asyncError })
            {
                Assert.IsType<AuthenticationFailedException>(error.InnerException);
                Assert.DoesNotContain("sentinel-cli-output", error.ToString());
            }
            Assert.Equal(2, credential.Calls);
            AssertNoLegacyReads(configuration);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task KnownCredentialFailuresExcludeCliOutput(bool unavailable)
        {
            const string unsafeMessage = "sentinel-cli-output-and-token";
            AuthenticationFailedException failure = unavailable
                ? new CredentialUnavailableException(unsafeMessage)
                : new AuthenticationFailedException(unsafeMessage);
            var failedCredential = new StubCredential(failure);
            var syncError = Assert.Throws<AuthenticationFailedException>(() =>
                DesignTimeContextFactory.GetPassword(failedCredential)
            );
            var asyncError = await Assert.ThrowsAsync<AuthenticationFailedException>(async () =>
                await DesignTimeContextFactory.GetPasswordAsync(
                    failedCredential,
                    CancellationToken.None
                )
            );
            foreach (var error in new[] { syncError, asyncError })
            {
                Assert.Contains("Azure CLI migration token acquisition failed", error.Message);
                Assert.DoesNotContain(unsafeMessage, error.ToString());
                Assert.Null(error.InnerException);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EmptyTokenCannotFallBackToEnvironmentOrPassfile(string? token)
        {
            var credential = new StubCredential
            {
                Result = token is null
                    ? default(AccessToken)
                    : new AccessToken(token, DateTimeOffset.MaxValue),
            };
            Assert.Throws<InvalidOperationException>(() =>
                DesignTimeContextFactory.GetPassword(credential)
            );
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await DesignTimeContextFactory.GetPasswordAsync(credential, CancellationToken.None)
            );
        }

        [Fact]
        public async Task UnexpectedErrorsPropagateWithoutFallback()
        {
            var failure = new InvalidOperationException("Synthetic programming error");
            var credential = new StubCredential(failure);
            Assert.Same(
                failure,
                Assert.Throws<InvalidOperationException>(() =>
                    DesignTimeContextFactory.GetPassword(credential)
                )
            );
            Assert.Same(
                failure,
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await DesignTimeContextFactory.GetPasswordAsync(
                        credential,
                        CancellationToken.None
                    )
                )
            );
        }

        [Fact]
        public async Task CancellationExcludesUnsafeExceptionTextAndPreservesCallerToken()
        {
            var credential = new StubCredential(
                new OperationCanceledException("sentinel-cli-output")
            );
            var syncError = Assert.Throws<OperationCanceledException>(() =>
                DesignTimeContextFactory.GetPassword(credential)
            );
            Assert.DoesNotContain("sentinel-cli-output", syncError.ToString());
            Assert.Null(syncError.InnerException);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var asyncError = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await DesignTimeContextFactory.GetPasswordAsync(credential, cancellation.Token)
            );
            Assert.Equal(cancellation.Token, asyncError.CancellationToken);
            Assert.Null(asyncError.InnerException);
        }

        [Fact]
        public async Task CallerCanCancelInFlightTokenAcquisition()
        {
            var credential = new StubCredential { WaitForCancellation = true };
            using var cancellation = new CancellationTokenSource();
            var acquisition = DesignTimeContextFactory
                .GetPasswordAsync(credential, cancellation.Token)
                .AsTask();
            Assert.False(acquisition.IsCompleted);
            cancellation.Cancel();
            var error = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await acquisition.WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken
                )
            );
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Null(error.InnerException);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DesignTimeContextOwnsItsIsolatedDataSource(bool asynchronous)
        {
            var configuration = new TrackingConfiguration();
            var credential = new StubCredential();
            var context = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                _ => credential
            );
            var source = Assert.IsAssignableFrom<NpgsqlDataSource>(Options(context).DataSource);
            using var other = DesignTimeContextFactory.CreateDbContext(
                configuration.Configuration,
                _ => credential
            );
            Assert.NotSame(source, Options(other).DataSource);
            if (asynchronous)
                await context.DisposeAsync();
            else
                context.Dispose();
            Assert.Throws<ObjectDisposedException>(() => source.OpenConnection());
            Assert.Equal(0, credential.Calls);
        }

        [Fact]
        public void CapabilityMarkerHasExactContractBytes()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (
                directory is not null
                && !Directory.Exists(Path.Combine(directory.FullName, ".github"))
            )
                directory = directory.Parent;
            Assert.NotNull(directory);
            Assert.Equal(
                System.Text.Encoding.UTF8.GetBytes("azure-cli-postgresql-v1\n"),
                File.ReadAllBytes(
                    Path.Combine(directory.FullName, "backend/api/.migration-auth-contract")
                )
            );
        }

        private static void AssertNoLegacyReads(TrackingConfiguration configuration) =>
            Assert.DoesNotContain(
                configuration.Reads,
                key =>
                    key.StartsWith("Database:", StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith("KeyVault:", StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith("AzureAd:", StringComparison.OrdinalIgnoreCase)
            );
    }
}
