using System.Reflection;
using Api.Database.Context;
using Api.Services.MissionLoaders;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.StackExchangeRedis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using StackExchange.Redis;

namespace Api.Configurations
{
    public static class CustomServiceConfigurations
    {
        private const string AzurePostgresScope =
            "https://ossrdbms-aad.database.windows.net/.default";

        public static IServiceCollection ConfigureDatabase(
            this IServiceCollection services,
            IConfiguration configuration,
            string environmentName
        )
        {
            Console.WriteLine("Configuring Database...");
            bool useInMemoryDatabase = configuration
                .GetSection("Database")
                .GetValue<bool>("UseInMemoryDatabase");

            if (environmentName.Equals("Test", StringComparison.Ordinal))
            {
                Console.WriteLine(
                    "The application is running in a test environment and database configuration is part of the test setup"
                );
            }
            else if (useInMemoryDatabase)
            {
                Console.WriteLine("Using InMemory Database");
                DbContextOptionsBuilder dbBuilder =
                    new DbContextOptionsBuilder<FlotillaDbContext>();
                string sqlConnectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = "file::memory:",
                    Cache = SqliteCacheMode.Shared,
                }.ToString();

                // In-memory sqlite requires an open connection throughout the whole lifetime of the database
                var connectionToInMemorySqlite = new SqliteConnection(sqlConnectionString);
                connectionToInMemorySqlite.Open();
                dbBuilder.UseSqlite(connectionToInMemorySqlite);

                using var context = new FlotillaDbContext(dbBuilder.Options);
                context.Database.EnsureCreated();
                bool initializeDb = configuration
                    .GetSection("Database")
                    .GetValue<bool>("InitializeInMemDb");
                if (initializeDb)
                    InitDb.PopulateDb(context);

                // Setting splitting behavior explicitly to avoid warning
                services.AddDbContext<FlotillaDbContext>(options =>
                    options.UseSqlite(
                        sqlConnectionString,
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                    )
                );
            }
            else
            {
                try
                {
                    Console.WriteLine("Trying Managed Identity for PostgreSQL…");
                    ConfigureDatabaseWithManagedIdentity(services, configuration);
                    Console.WriteLine("Managed Identity configured successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Managed Identity failed. Falling back to Key Vault. Reason: {ex.GetType().Name}: {ex.Message}"
                    );
                    ConfigureDatabaseWithKeyvaultConnString(services, configuration);
                }
            }
            return services;
        }

        public static void ConfigureDatabaseWithManagedIdentity(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var server =
                configuration["Database:Server"]
                ?? throw new InvalidOperationException("Missing Database:Server");
            var postgresDb =
                configuration["Database:PostgresDatabase"]
                ?? throw new InvalidOperationException("Missing Database:PostgresDatabase");
            var dbUser =
                configuration["Database:User"]
                ?? throw new InvalidOperationException("Missing Database:User");

            var credential = CreateCredential(configuration);

            Console.WriteLine("Requesting Entra token via Credential...");
            TokenRequestContext context = new([AzurePostgresScope]);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            AccessToken token;
            try
            {
                token = credential.GetToken(context, cts.Token);
            }
            catch (OperationCanceledException oce)
            {
                throw new TimeoutException("Timed out acquiring token", oce);
            }

            var baseConnString = new NpgsqlConnectionStringBuilder
            {
                Host = $"{server}.postgres.database.azure.com",
                Database = postgresDb,
                Username = $"{dbUser}",
                SslMode = SslMode.VerifyFull,
            }.ToString();

            int DATABASE_TIMEOUT;
            var timeoutValue = configuration["Database:Timeout"];
            if (
                !string.IsNullOrEmpty(timeoutValue)
                && int.TryParse(timeoutValue, out var parsedTimeout)
            )
            {
                DATABASE_TIMEOUT = parsedTimeout;
            }
            else
            {
                DATABASE_TIMEOUT = 30;
            }
            // Setting splitting behavior explicitly to avoid warning
            services.AddDbContext<FlotillaDbContext>(
                options =>
                    options.UseNpgsql(
                        baseConnString,
                        o =>
                        {
                            o.ConfigureDataSource(ds =>
                            {
                                var dbCredential = CreateCredential(configuration);
                                ds.UsePeriodicPasswordProvider(
                                    async (_, ct) =>
                                    {
                                        using var cts = new CancellationTokenSource(
                                            TimeSpan.FromSeconds(5)
                                        );
                                        var token = await dbCredential.GetTokenAsync(
                                            new TokenRequestContext([AzurePostgresScope]),
                                            CancellationTokenSource
                                                .CreateLinkedTokenSource(ct, cts.Token)
                                                .Token
                                        );
                                        return token.Token;
                                    },
                                    successRefreshInterval: TimeSpan.FromMinutes(55),
                                    failureRefreshInterval: TimeSpan.FromSeconds(5)
                                );
                            });
                            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                            o.EnableRetryOnFailure();
                            o.CommandTimeout(DATABASE_TIMEOUT);
                        }
                    ),
                ServiceLifetime.Scoped
            );
        }

        private static TokenCredential CreateCredential(IConfiguration config)
        {
            string? tenantId = config["AzureAd:TenantId"];
            string? clientId = config["AzureAd:ClientId"];
            string? clientSecret = config["AzureAd:ClientSecret"];

            tenantId ??= config["AZURE_TENANT_ID"];
            clientId ??= config["AZURE_CLIENT_ID"];
            clientSecret ??= config["AZURE_CLIENT_SECRET"];

            var workloadOptions = new WorkloadIdentityCredentialOptions();
            if (!string.IsNullOrWhiteSpace(clientId))
                workloadOptions.ClientId = clientId;
            if (!string.IsNullOrWhiteSpace(tenantId))
                workloadOptions.TenantId = tenantId;

            var workloadIdentity = new WorkloadIdentityCredential(workloadOptions);

            bool allowUsingClientSecret = config.GetValue<bool>("AllowUsingClientSecret");

            if (
                allowUsingClientSecret
                && !string.IsNullOrWhiteSpace(tenantId)
                && !string.IsNullOrWhiteSpace(clientId)
                && !string.IsNullOrWhiteSpace(clientSecret)
                && !clientSecret.StartsWith("Fill in", StringComparison.OrdinalIgnoreCase)
            )
            {
                Console.WriteLine(
                    "Using ChainedTokenCredential: WorkloadIdentityCredential -> ClientSecretCredential"
                );
                return new ChainedTokenCredential(
                    workloadIdentity,
                    new ClientSecretCredential(tenantId, clientId, clientSecret)
                );
            }

            Console.WriteLine("Using WorkloadIdentityCredential only");
            return workloadIdentity;
        }

        public static void ConfigureDatabaseWithKeyvaultConnString(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            string? connection = configuration["Database:PostgreSqlConnectionString"];
            int DATABASE_TIMEOUT;
            var timeoutValue = configuration["Database:Timeout"];
            if (
                !string.IsNullOrEmpty(timeoutValue)
                && int.TryParse(timeoutValue, out var parsedTimeout)
            )
            {
                DATABASE_TIMEOUT = parsedTimeout;
            }
            else
            {
                DATABASE_TIMEOUT = 30;
            }
            // Setting splitting behavior explicitly to avoid warning
            services.AddDbContext<FlotillaDbContext>(
                options =>
                    options.UseNpgsql(
                        connection,
                        o =>
                        {
                            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                            o.EnableRetryOnFailure();
                            o.CommandTimeout(DATABASE_TIMEOUT);
                        }
                    ),
                ServiceLifetime.Scoped
            );
        }

        public static IServiceCollection ConfigureSwagger(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddSwaggerGen(c =>
            {
                // Add Authorization button in UI
                c.AddSecurityDefinition(
                    "oauth2",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(
                                    $"{configuration["AzureAd:Instance"]}/{configuration["AzureAd:TenantId"]}/oauth2/token"
                                ),
                                AuthorizationUrl = new Uri(
                                    $"{configuration["AzureAd:Instance"]}/{configuration["AzureAd:TenantId"]}/oauth2/authorize"
                                ),
                                Scopes = new Dictionary<string, string>
                                {
                                    {
                                        $"api://{configuration["AzureAd:ClientId"]}/user_impersonation",
                                        "User Impersonation"
                                    },
                                },
                            },
                        },
                    }
                );

                // Show which endpoints have authorization in the UI
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("oauth2", document)] = [],
                });

                // Make swagger use xml comments from functions
                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        public static IServiceCollection ConfigureMissionLoader(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            string? missionLoaderFileName = configuration["MissionLoader:FileName"];
            if (missionLoaderFileName == null)
                return services;

            var loaderType = Type.GetType(missionLoaderFileName);
            if (loaderType != null && typeof(IMissionLoader).IsAssignableFrom(loaderType))
            {
                services.AddScoped(typeof(IMissionLoader), loaderType);
            }
            else
            {
                throw new InvalidOperationException(
                    "The specified class does not implement IMissionLoader or could not be found."
                );
            }

            return services;
        }

        public static IServiceCollection ConfigureRedisCache(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddStackExchangeRedisCache(async options =>
            {
                var redisHostName = configuration["Redis:HostName"];
                var redisPort = configuration.GetValue<int>("Redis:Port");
                var useSsl = configuration.GetValue<bool>("Redis:UseSsl");

                if (string.IsNullOrEmpty(redisHostName))
                {
                    throw new InvalidOperationException(
                        "Redis:HostName configuration is required for Entra authentication"
                    );
                }

                var configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { { redisHostName, redisPort } },
                    Ssl = useSsl,
                    AbortOnConnectFail = false,
                };

                await configurationOptions.ConfigureForAzureWithServicePrincipalAsync(
                    clientId: configuration["AzureAd:ClientId"]!,
                    tenantId: configuration["AzureAd:TenantId"]!,
                    secret: configuration["AzureAd:ClientSecret"]!
                );

                options.ConfigurationOptions = configurationOptions;
                options.InstanceName = configuration.GetValue<string>(
                    "Redis:InstanceName",
                    "FlotillaCache"
                );
            });

            return services;
        }
    }
}
