using System.Reflection;
using Api.Database.Context;
using Api.Services.MissionLoaders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Api.Configurations
{
    public static class CustomServiceConfigurations
    {
        public static IServiceCollection ConfigureDatabase(
            this IServiceCollection services,
            IConfiguration configuration,
            string environmentName
        )
        {
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
                string? connection = configuration["Database:PostgreSqlConnectionString"];
                // Setting splitting behavior explicitly to avoid warning
                services.AddDbContext<FlotillaDbContext>(
                    options =>
                        options.UseNpgsql(
                            connection,
                            o =>
                            {
                                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                                o.EnableRetryOnFailure();
                            }
                        ),
                    ServiceLifetime.Transient
                );
            }
            return services;
        }

        public static IServiceCollection ConfigureSwagger(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            bool disableAuth = configuration.GetValue<bool>("DisableAuth");
            if (disableAuth)
            {
                Console.WriteLine("Swagger OAuth is disabled.");
                // Ensure no OAuth configuration is applied
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Flotilla API", Version = "v1" });
                    // Make swagger use xml comments from functions
                    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });
                return services;
            }
            else
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
                    c.AddSecurityRequirement(
                        new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "oauth2",
                                    },
                                },
                                Array.Empty<string>()
                            },
                        }
                    );

                    // Make swagger use xml comments from functions
                    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });
            }

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

            try
            {
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
            }
            catch (Exception)
            {
                throw;
            }
            return services;
        }
    }
}
