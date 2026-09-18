using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Configurations;
using Api.Database.Context;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Xunit;

// Inspect provider options without creating a data source or opening a database connection.
#pragma warning disable EF1001

namespace Api.Test.Database
{
    public class DatabaseConfigurationsTests
    {
        private const string PasswordKey = "Database:PostgreSqlConnectionString";
        private const string PasswordConnection =
            "Host=localhost;Database=fallback;Username=local;Password=dummy-password";

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
                    environmentName,
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
                "Production",
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
                    environmentName,
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
                environmentName,
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

            services.ConfigureDatabase(configuration.BuildConfiguration(), "Test", credential);

            Assert.Empty(services);
            Assert.Equal(0, credential.SyncCalls);
            Assert.Equal(0, credential.AsyncCalls);
            Assert.DoesNotContain(PasswordKey, configuration.Reads);
            Assert.DoesNotContain("Database:SeedExampleDataPostgres", configuration.Reads);
        }
    }
}
