using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Controllers;
using Api.Controllers.Models;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Options;
using Api.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"\nENVIRONMENT IS SET TO '{builder.Environment.EnvironmentName}'\n");

builder.AddAzureEnvironmentVariables();

if (builder.Configuration.GetSection("KeyVault").GetValue<bool>("UseKeyVault"))
{
    // The ExcludeSharedTokenCacheCredential option is a recommended workaround by Azure for dockerization
    // See https://github.com/Azure/azure-sdk-for-net/issues/17052
    builder.Configuration.AddAzureKeyVault(
        new Uri(builder.Configuration.GetSection("KeyVault")["VaultUri"]),
        new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ExcludeSharedTokenCacheCredential = true
            }
        )
    );
}

builder.ConfigureLogger();

builder.Services.ConfigureDatabase(builder.Configuration);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddScoped<IRobotService, RobotService>();
builder.Services.AddScoped<IMissionRunService, MissionRunService>();
builder.Services.AddScoped<IMissionDefinitionService, MissionDefinitionService>();
builder.Services.AddScoped<IIsarService, IsarService>();
builder.Services.AddScoped<IEchoService, EchoService>();
builder.Services.AddScoped<IStidService, StidService>();
builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IRobotModelService, RobotModelService>();
builder.Services.AddScoped<RobotController>();
builder.Services.AddScoped<ICustomMissionService, CustomMissionService>();

builder.Services.AddHostedService<MqttEventHandler>();
builder.Services.AddHostedService<MqttService>();
builder.Services.AddHostedService<IsarConnectionEventHandler>();
builder.Services.AddHostedService<MissionScheduler>();

builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection("AzureAd"));
builder.Services.Configure<MapBlobOptions>(builder.Configuration.GetSection("Maps"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Blob"));

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
            .Build();
    }
);

var app = builder.Build();

string basePath = builder.Configuration["BackendBaseRoute"];
app.UseSwagger(
    c =>
    {
        c.PreSerializeFilters.Add(
            (swaggerDoc, httpReq) =>
            {
                swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new()
                    {
                        Url = $"https://{httpReq.Host.Value}{basePath}"
                    },
                    new()
                    {
                        Url = $"http://{httpReq.Host.Value}{basePath}"
                    }
                };
            }
        );
    }
);
app.UseSwaggerUI(
    c =>
    {
        c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
        // The following parameter represents the "audience" of the access token.
        c.OAuthAdditionalQueryStringParams(
            new Dictionary<string, string>
            {
                {
                    "Resource", builder.Configuration["AzureAd:ClientId"]
                }
            }
        );
        c.OAuthUsePkce();
    }
);

var option = new RewriteOptions();
option.AddRedirect("^$", "swagger");
app.UseRewriter(option);

app.UseCors(
    corsBuilder =>
        corsBuilder
            .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>())
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithExposedHeaders(QueryStringParameters.PaginationHeader)
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
