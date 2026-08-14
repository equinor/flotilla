namespace Api.Configurations
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the logger used by the application
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static void ConfigureLogger(this WebApplicationBuilder builder)
        {
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss - ";
                options.ColorBehavior = Microsoft
                    .Extensions
                    .Logging
                    .Console
                    .LoggerColorBehavior
                    .Enabled;
            });
        }
    }
}
