namespace Api.Configurations
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Creates the AZURE_CLIENT_ID and AZURE_TENANT_ID configuration values for the
        /// <see href="https://docs.microsoft.com/en-us/dotnet/api/azure.identity.environmentcredential?view=azure-dotnet">Environment Credentials</see>
        /// used by the application when dockerized.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static void AddAzureEnvironmentVariables(this WebApplicationBuilder builder)
        {
            string? clientId = builder.Configuration
                .GetSection("AzureAd")
                .GetValue<string?>("ClientId");
            if (clientId is not null)
            {
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", clientId);
                Console.WriteLine("'AZURE_CLIENT_ID' set to " + clientId);
            }

            string? tenantId = builder.Configuration
                .GetSection("AzureAd")
                .GetValue<string?>("TenantId");
            if (clientId is not null)
            {
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", tenantId);
                Console.WriteLine("'AZURE_TENANT_ID' set to " + tenantId);
            }
        }

        /// <summary>
        /// Configures the logger used by the application
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static void ConfigureLogger(this WebApplicationBuilder builder)
        {
            builder.Logging.AddSimpleConsole(
                options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss - ";
                    options.ColorBehavior = Microsoft
                        .Extensions
                        .Logging
                        .Console
                        .LoggerColorBehavior
                        .Enabled;
                }
            );
        }
    }
}
