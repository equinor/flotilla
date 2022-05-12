using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Api.Configurations
{
    public static class CustomServiceConfigurations
    {
        public static IServiceCollection ConfigureSwagger(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddSwaggerGen(
                c =>
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
                                            "api://ea4c7b92-47b3-45fb-bd25-a8070f0c495c/user_impersonation",
                                            "User Impersonation"
                                        }
                                    },
                                }
                            }
                        }
                    );
                    // Show which endpoints have authorization in the UI
                    c.AddSecurityRequirement(
                        new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme()
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "oauth2"
                                    }
                                },
                                Array.Empty<string>()
                            }
                        }
                    );

                    // Make swagger use xml comments from functions
                    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                }
            );

            return services;
        }
    }
}
