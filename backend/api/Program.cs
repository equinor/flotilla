using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Controllers;
using Api.Controllers.Models;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Options;
using Api.Services;
using Api.Services.ActionServices;
using Api.SignalRHubs;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
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
    string? vaultUri = builder.Configuration.GetSection("KeyVault")["VaultUri"];
    if (!string.IsNullOrEmpty(vaultUri))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(vaultUri),
            new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    ExcludeSharedTokenCacheCredential = true
                }
            )
        );
    }
    else
    {
        Console.WriteLine("NO KEYVAULT IN CONFIG");
    }
}

builder.ConfigureLogger();

builder.Services.ConfigureDatabase(builder.Configuration);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddScoped<IRobotService, RobotService>();
builder.Services.AddScoped<IMissionRunService, MissionRunService>();
builder.Services.AddScoped<IInspectionService, InspectionService>();
builder.Services.AddScoped<IEmergencyActionService, EmergencyActionService>();
builder.Services.AddScoped<IMissionDefinitionService, MissionDefinitionService>();
builder.Services.AddScoped<IIsarService, IsarService>();
builder.Services.AddScoped<IEchoService, EchoService>();
builder.Services.AddScoped<IStidService, StidService>();
builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IDefaultLocalizationPoseService, DefaultLocalizationPoseService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<IRobotModelService, RobotModelService>();
builder.Services.AddScoped<IMissionSchedulingService, MissionSchedulingService>();
builder.Services.AddScoped<ICustomMissionSchedulingService, CustomMissionSchedulingService>();
builder.Services.AddScoped<ITaskDurationService, TaskDurationService>();
builder.Services.AddScoped<IPoseTimeseriesService, PoseTimeseriesService>();
builder.Services.AddScoped<ILastMissionRunService, LastMissionRunService>();


bool useInMemoryDatabase = builder.Configuration
    .GetSection("Database")
    .GetValue<bool>("UseInMemoryDatabase");

if (useInMemoryDatabase)
{
    builder.Services.AddScoped<ITimeseriesService, TimeseriesServiceSqlLite>();
}
else
{
    builder.Services.AddScoped<ITimeseriesService, TimeseriesService>();
}
builder.Services.AddScoped<RobotController>();
builder.Services.AddScoped<EmergencyActionController>();
builder.Services.AddScoped<ICustomMissionService, CustomMissionService>();

builder.Services.AddTransient<ISignalRService, SignalRService>();

builder.Services.AddHostedService<MqttEventHandler>();
builder.Services.AddHostedService<MissionEventHandler>();
builder.Services.AddHostedService<MqttService>();
builder.Services.AddHostedService<IsarConnectionEventHandler>();

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
    .AddDownstreamApi(EchoService.ServiceName, builder.Configuration.GetSection("Echo"))
    .AddDownstreamApi(StidService.ServiceName, builder.Configuration.GetSection("Stid"))
    .AddDownstreamApi(IsarService.ServiceName, builder.Configuration.GetSection("Isar"));

builder.Services.AddAuthorization(
    options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
);

builder.Services.AddSignalR();

var app = builder.Build();
string basePath = builder.Configuration["BackendBaseRoute"] ?? "";
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
                    "Resource", builder.Configuration["AzureAd:ClientId"] ?? throw new ArgumentException("No Azure Ad ClientId")
                }
            }
        );
        c.OAuthUsePkce();
    }
);

var option = new RewriteOptions();
option.AddRedirect("^$", "swagger");
app.UseRewriter(option);

string[] allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
app.UseCors(
    corsBuilder =>
        corsBuilder
            .WithOrigins(allowedOrigins)
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithExposedHeaders(QueryStringParameters.PaginationHeader)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<SignalRHub>("/hub", options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
});

app.MapControllers();

app.Run();

#pragma warning disable CA1050
public partial class Program { }
#pragma warning restore CA1050
