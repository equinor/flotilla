using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Api.Configurations;
using Api.Database.Context;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Xunit;

// Inspect provider options without creating a data source or opening a database connection.
#pragma warning disable EF1001

namespace Api.Test.Database
{
    [Collection("Postgres diagnostics")]
    public class DatabaseConfigurationsTests
    {
        private const string PasswordKey = "Database:PostgreSqlConnectionString";
        private const string PasswordConnection =
            "Host=localhost;Database=fallback;Username=local;Password=dummy-password";

        private static IHostEnvironment Host(string name) =>
            Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == name);

        private sealed class TrackingConfiguration : ConfigurationProvider, IConfigurationSource
        {
            public HashSet<string> Reads { get; } = new(StringComparer.OrdinalIgnoreCase);

            public TrackingConfiguration()
            {
                Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Database:Server"] = "test-server",
                    ["Database:PostgresDatabase"] = "test-db",
                    ["Database:User"] = "test-identity",
                    [PasswordKey] = PasswordConnection,
                    ["AzureAd:AllowUsingClientSecret"] = "true",
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["environment"] = "Development",
                };
            }

            public override bool TryGet(string key, out string? value)
            {
                Reads.Add(key);
                return base.TryGet(key, out value);
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

            public IConfigurationRoot BuildConfiguration() =>
                new ConfigurationBuilder().Add(this).Build();
        }

        private sealed class StubTokenCredential(Exception? failure = null) : TokenCredential
        {
            public int SyncCalls { get; private set; }
            public int AsyncCalls { get; private set; }

            public override AccessToken GetToken(
                TokenRequestContext requestContext,
                CancellationToken cancellationToken
            )
            {
                SyncCalls++;
                Assert.Equal(
                    ["https://ossrdbms-aad.database.windows.net/.default"],
                    requestContext.Scopes
                );
                Assert.True(cancellationToken.CanBeCanceled);
                if (failure is not null)
                    throw failure;
                return new AccessToken("dummy-token", DateTimeOffset.UtcNow.AddHours(1));
            }

            public override ValueTask<AccessToken> GetTokenAsync(
                TokenRequestContext requestContext,
                CancellationToken cancellationToken
            )
            {
                AsyncCalls++;
                return failure is not null
                    ? ValueTask.FromException<AccessToken>(failure)
                    : ValueTask.FromResult(
                        new AccessToken("dummy-token", DateTimeOffset.UtcNow.AddHours(1))
                    );
            }
        }

        [Theory]
        [InlineData("Staging", null)]
        [InlineData("pRoDuCtIoN", null)]
        [InlineData("Staging", "Database:Server")]
        public void ProtectedEnvironmentRejectsSetupFailureWithoutPasswordAccess(
            string environmentName,
            string? missingKey
        )
        {
            var configuration = new TrackingConfiguration();
            if (missingKey is not null)
                configuration.Set(missingKey, null);
            var services = new ServiceCollection();
            var failure = new InvalidOperationException("token acquisition failed");
            var credential = new StubTokenCredential(failure);

            var thrown = Assert.Throws<InvalidOperationException>(() =>
                services.ConfigureDatabase(
                    configuration.BuildConfiguration(),
                    Host(environmentName),
                    credential
                )
            );

            if (missingKey is null)
                Assert.Same(failure, thrown);
            else
                Assert.Equal($"Missing {missingKey}", thrown.Message);
            Assert.Equal(missingKey is null ? 1 : 0, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
            Assert.Empty(services);
            Assert.DoesNotContain(PasswordKey, configuration.Reads);
        }

        [Fact]
        public void ProtectedEnvironmentConfiguresTokenProviderWithoutPasswordAccess()
        {
            var configuration = new TrackingConfiguration();
            var services = new ServiceCollection();
            var credential = new StubTokenCredential();

            services.ConfigureDatabase(
                configuration.BuildConfiguration(),
                Host("Production"),
                credential
            );

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DbContextOptions<FlotillaDbContext>>();
            var npgsqlOptions = options.GetExtension<NpgsqlOptionsExtension>();
            var connection = new NpgsqlConnectionStringBuilder(npgsqlOptions.ConnectionString);
            Assert.Equal("test-server.postgres.database.azure.com", connection.Host);
            Assert.Equal("test-db", connection.Database);
            Assert.Equal("test-identity", connection.Username);
            Assert.True(string.IsNullOrEmpty(connection.Password));
            Assert.NotNull(npgsqlOptions.DataSourceBuilderAction);
            Assert.Equal(1, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
            Assert.DoesNotContain(PasswordKey, configuration.Reads);
        }

        [Theory]
        [InlineData("sTaGiNg")]
        [InlineData("Production")]
        public void ProtectedEnvironmentRejectsSeedingBeforeTokenOrDatabaseSetup(
            string environmentName
        )
        {
            var configuration = new TrackingConfiguration();
            configuration.Set("Database:SeedExampleDataPostgres", "true");
            var services = new ServiceCollection();
            var credential = new StubTokenCredential();

            var thrown = Assert.Throws<InvalidOperationException>(() =>
                services.ConfigureDatabase(
                    configuration.BuildConfiguration(),
                    Host(environmentName),
                    credential
                )
            );

            Assert.Contains($"[{environmentName}]", thrown.Message);
            Assert.Contains("Database:SeedExampleDataPostgres is not supported", thrown.Message);
            Assert.Equal(0, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
            Assert.Empty(services);
            Assert.DoesNotContain("Database:Server", configuration.Reads);
            Assert.DoesNotContain(PasswordKey, configuration.Reads);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Local")]
        [InlineData("IntegrationTest")]
        public void OtherEnvironmentsRetainPasswordFallback(string environmentName)
        {
            var configuration = new TrackingConfiguration();
            var services = new ServiceCollection();
            var credential = new StubTokenCredential(new InvalidOperationException("no token"));

            services.ConfigureDatabase(
                configuration.BuildConfiguration(),
                Host(environmentName),
                credential
            );

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DbContextOptions<FlotillaDbContext>>();
            var npgsqlOptions = options.GetExtension<NpgsqlOptionsExtension>();
            var connection = new NpgsqlConnectionStringBuilder(npgsqlOptions.ConnectionString);
            Assert.Equal("localhost", connection.Host);
            Assert.Equal("fallback", connection.Database);
            Assert.Equal("local", connection.Username);
            Assert.Equal("dummy-password", connection.Password);
            Assert.Null(npgsqlOptions.DataSourceBuilderAction);
            Assert.Contains(PasswordKey, configuration.Reads);
            Assert.Equal(1, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
        }

        [Fact]
        public void ExactTestEnvironmentSkipsDatabaseConfigurationBeforeContainerBranch()
        {
            var configuration = new TrackingConfiguration();
            configuration.Set("Database:UseInMemoryDatabase", "true");
            configuration.Set("Database:SeedExampleDataPostgres", "true");
            var services = new ServiceCollection();
            var credential = new StubTokenCredential();

            services.ConfigureDatabase(
                configuration.BuildConfiguration(),
                Host("Test"),
                credential
            );

            Assert.Empty(services);
            Assert.Equal(0, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
            Assert.DoesNotContain(PasswordKey, configuration.Reads);
            Assert.DoesNotContain("Database:SeedExampleDataPostgres", configuration.Reads);
        }

        [Theory]
        [InlineData("Development", false, false)]
        [InlineData("Development", true, true)]
        [InlineData("dEvElOpMeNt", true, true)]
        [InlineData("sTaGiNg", true, false)]
        [InlineData("pRoDuCtIoN", true, false)]
        [InlineData("Local", true, false)]
        [InlineData("IntegrationTest", true, false)]
        public async Task OnlyEnabledDevelopmentInstallsBothPhysicalHooksOnExistingProvider(
            string environment,
            bool enabled,
            bool expectHooks
        )
        {
            var configuration = new TrackingConfiguration();
            configuration.Set(PostgresTokenDiagnostics.EnabledKey, enabled.ToString());
            var services = new ServiceCollection();
            services.AddLogging();
            var credential = new StubTokenCredential();
            services.ConfigureDatabase(
                configuration.BuildConfiguration(),
                Host(environment),
                credential
            );
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DbContextOptions<FlotillaDbContext>>();
            var npgsql = options.GetExtension<NpgsqlOptionsExtension>();
            var builder = new NpgsqlDataSourceBuilder(npgsql.ConnectionString);
            Assert.NotNull(npgsql.DataSourceBuilderAction);
            npgsql.DataSourceBuilderAction(builder);
            // Build only: Npgsql starts the synthetic credential callback, but no connection is opened.
            await using var source = builder.Build();
            var sync = ReadSourceProperty(source, "ConnectionInitializer");
            var async = ReadSourceProperty(source, "ConnectionInitializerAsync");
            if (expectHooks)
            {
                var syncHook = Assert.IsType<Action<NpgsqlConnection>>(sync);
                var asyncHook = Assert.IsType<Func<NpgsqlConnection, Task>>(async);
                Assert.IsType<PostgresTokenDiagnostics>(syncHook.Target);
                Assert.Same(syncHook.Target, asyncHook.Target);
                using var closed = new NpgsqlConnection();
                Assert.Throws<InvalidOperationException>(() => syncHook(closed));
                await Assert.ThrowsAsync<InvalidOperationException>(() => asyncHook(closed));
            }
            else
            {
                Assert.Null(sync);
                Assert.Null(async);
            }
            var callback = Assert.IsType<
                Func<NpgsqlConnectionStringBuilder, CancellationToken, ValueTask<string>>
            >(ReadSourceField(source, "_periodicPasswordProvider"));
            Assert.Equal(
                "dummy-token",
                await callback(new(), TestContext.Current.CancellationToken)
            );
            Assert.Equal(
                TimeSpan.FromMinutes(55),
                ReadSourceField(source, "_periodicPasswordSuccessRefreshInterval")
            );
            Assert.Equal(
                TimeSpan.FromSeconds(5),
                ReadSourceField(source, "_periodicPasswordFailureRefreshInterval")
            );
            Assert.Equal(1, credential.SyncCalls);
            Assert.DoesNotContain(PasswordKey, configuration.Reads);
        }

        // Inspect pinned Npgsql 10.0.2 registrations without network I/O or exposing production hooks.
        private static object? ReadSourceProperty(NpgsqlDataSource source, string name) =>
            typeof(NpgsqlDataSource)
                .GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(source);

        private static object? ReadSourceField(NpgsqlDataSource source, string name) =>
            typeof(NpgsqlDataSource)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(source);

        [Fact]
        public void EnabledDevelopmentFallbackIsExplicitlyUnavailableWithoutLeakingFailure()
        {
            var configuration = new TrackingConfiguration();
            configuration.Set(PostgresTokenDiagnostics.EnabledKey, "true");
            var services = new ServiceCollection();
            var original = Console.Out;
            using var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                services.ConfigureDatabase(
                    configuration.BuildConfiguration(),
                    Host("Development"),
                    new StubTokenCredential(
                        new InvalidOperationException("SENTINEL-TOKEN " + PasswordConnection)
                    )
                );
            }
            finally
            {
                Console.SetOut(original);
            }
            Assert.Contains(
                "Unavailable: ConnectionStringFallback; token login not tested",
                output.ToString()
            );
            Assert.DoesNotContain("SENTINEL-TOKEN", output.ToString());
            Assert.DoesNotContain(PasswordConnection, output.ToString());
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DbContextOptions<FlotillaDbContext>>();
            var npgsql = options.GetExtension<NpgsqlOptionsExtension>();
            Assert.Null(npgsql.DataSourceBuilderAction);
            var connection = new NpgsqlConnectionStringBuilder(npgsql.ConnectionString);
            Assert.Equal("localhost", connection.Host);
            Assert.Equal("fallback", connection.Database);
            Assert.Equal("local", connection.Username);
            Assert.Equal("dummy-password", connection.Password);
        }

        [Fact]
        public void EnabledFlagDoesNotChangeExactTestSkipOrStartContainer()
        {
            var configuration = new TrackingConfiguration();
            configuration.Set(PostgresTokenDiagnostics.EnabledKey, "true");
            configuration.Set("Database:UseInMemoryDatabase", "true");
            var services = new ServiceCollection();
            var credential = new StubTokenCredential();
            services.ConfigureDatabase(
                configuration.BuildConfiguration(),
                Host("Test"),
                credential
            );
            Assert.Empty(services);
            Assert.Equal(0, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
        }
    }
}
