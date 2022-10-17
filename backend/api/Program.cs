using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Controllers;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Services;
using Api.Utilities;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureEnvironmentVariables();
builder.ConfigureLogger();

builder.Services.ConfigureDatabase(builder.Configuration);

builder.Services.AddScoped<IRobotService, RobotService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IIsarService, IsarService>();
builder.Services.AddScoped<IEchoService, EchoService>();
builder.Services.AddScoped<IStidService, StidService>();
builder.Services.AddScoped<ITagPositioner, TagPositioner>();
builder.Services.AddScoped<RobotController>();

builder.Services.AddHostedService<MqttEventHandler>();
builder.Services.AddHostedService<MqttService>();
builder.Services.AddHostedService<MissionScheduler>();

builder.Services
    .AddControllers()
    .AddJsonOptions(
        options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        }
    );

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches()
    .AddDownstreamWebApi(EchoService.ServiceName, builder.Configuration.GetSection("Echo"))
    .AddDownstreamWebApi(StidService.ServiceName, builder.Configuration.GetSection("Stid"))
    .AddDownstreamWebApi(IsarService.ServiceName, builder.Configuration.GetSection("Isar"));

builder.Services.AddAuthorization(
    options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(builder.Configuration.GetSection("Authorization")["Roles"])
            .Build();
    }
);

// The ExcludeSharedTokenCacheCredential option is a recommended workaround by Azure for dockerization
// See https://github.com/Azure/azure-sdk-for-net/issues/17052
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration.GetSection("KeyVault")["VaultUri"]),
    new DefaultAzureCredential(
        new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true }
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        c =>
        {
            c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
            // The following parameter represents the "audience" of the access token.
            c.OAuthAdditionalQueryStringParams(
                new Dictionary<string, string>
                {
                    { "Resource", builder.Configuration["AzureAd:ClientId"] }
                }
            );
            c.OAuthUsePkce();
        }
    );
}

app.UseCors(
    corsBuilder =>
        corsBuilder
            .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>())
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

#pragma warning disable CA1050
public partial class Program { }
#pragma warning restore CA1050
