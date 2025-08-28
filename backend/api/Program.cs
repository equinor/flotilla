using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Controllers;
using Api.Controllers.Models;
using Api.EventHandlers;
using Api.HostedServices;
using Api.Mqtt;
using Api.Options;
using Api.Services;
using Api.Services.ActionServices;
using Api.SignalRHubs;
using Api.Utilities;
using Azure.Identity;
using Hangfire;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"\nENVIRONMENT IS SET TO '{builder.Environment.EnvironmentName}'\n");

builder.AddAppSettingsEnvironmentVariables();
builder.AddDotEnvironmentVariables(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

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
                new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true }
            )
        );
    }
    else
    {
        Console.WriteLine("NO KEYVAULT IN CONFIG");
    }
}

var applicationName = builder.Configuration["AppName"] ?? "FlotillaBackend";

builder.ConfigureLogger();

builder.Services.ConfigureDatabase(builder.Configuration, builder.Environment.EnvironmentName);

builder.Services.ConfigureMissionLoader(builder.Configuration);

var openTelemetryEnabled = builder.Configuration.GetValue<bool?>("OpenTelemetry:Enabled") ?? false;
var otelActivitySource = new ActivitySource(applicationName);
var otelMeter = new Meter($"{applicationName}.Metrics", "0.0.1");
if (openTelemetryEnabled)
{
    builder.AddCustomOpenTelemetry(otelActivitySource, otelMeter);
}
else
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddScoped<IAccessRoleService, AccessRoleService>();

builder.Services.AddScoped<IRobotService, RobotService>();
builder.Services.AddScoped<IRobotModelService, RobotModelService>();

builder.Services.AddScoped<IMissionRunService, MissionRunService>();
builder.Services.AddScoped<IMissionDefinitionService, MissionDefinitionService>();
builder.Services.AddScoped<IMissionDefinitionTaskService, MissionDefinitionTaskService>();
builder.Services.AddScoped<IMissionTaskService, MissionTaskService>();
builder.Services.AddScoped<IInspectionService, InspectionService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<IExclusionAreaService, ExclusionAreaService>();

builder.Services.AddScoped<IMissionSchedulingService, MissionSchedulingService>();

builder.Services.AddScoped<IIsarService, IsarService>();
builder.Services.AddScoped<IEchoService, EchoService>();

builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IBlobService, BlobService>();

builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<IInspectionAreaService, InspectionAreaService>();
builder.Services.AddScoped<IAreaPolygonService, AreaPolygonService>();

builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddScoped<ITaskDurationService, TaskDurationService>();
builder.Services.AddScoped<ILastMissionRunService, LastMissionRunService>();
builder.Services.AddScoped<IEmergencyActionService, EmergencyActionService>();
builder.Services.AddScoped<ITeamsMessageService, TeamsMessageService>();

bool useInMemoryDatabase = builder
    .Configuration.GetSection("Database")
    .GetValue<bool>("UseInMemoryDatabase");

builder.Services.AddScoped<RobotController>();
builder.Services.AddScoped<EmergencyActionController>();
builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddScoped<IAutoScheduleService, AutoScheduleService>();

builder.Services.AddTransient<ISignalRService, SignalRService>();

builder.Services.AddHostedService<MqttEventHandler>();
builder.Services.AddHostedService<MissionEventHandler>();
builder.Services.AddHostedService<MqttService>();
builder.Services.AddHostedService<IsarConnectionEventHandler>();
builder.Services.AddHostedService<TeamsMessageEventHandler>();
builder.Services.AddHostedService<AutoSchedulingHostedService>();

builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection("AzureAd"));
builder.Services.Configure<MapBlobOptions>(builder.Configuration.GetSection("Maps"));

builder.Services.AddHangfire(Configuration => Configuration.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger(builder.Configuration);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches()
    .AddDownstreamApi(EchoService.ServiceName, builder.Configuration.GetSection("Echo"))
    .AddDownstreamApi(InspectionService.ServiceName, builder.Configuration.GetSection("SARA"))
    .AddDownstreamApi(IsarService.ServiceName, builder.Configuration.GetSection("Isar"));

builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.Events ??= new JwtBearerEvents();
        options.Events.OnMessageReceived = context =>
        {
            if (
                context.HttpContext.Request.Path.StartsWithSegments("/hub")
                && context.Request.Query.TryGetValue("access_token", out var token)
            )
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        };
    }
);

builder
    .Services.AddAuthorizationBuilder()
    .AddFallbackPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());

builder.Services.AddSignalR();

var app = builder.Build();
string basePath = builder.Configuration["BackendBaseRoute"] ?? "";
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add(
        (swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers =
            [
                new() { Url = $"https://{httpReq.Host.Value}{basePath}" },
                new() { Url = $"http://{httpReq.Host.Value}{basePath}" },
            ];
        }
    );
});
app.UseSwaggerUI(c =>
{
    c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
    // The following parameter represents the "audience" of the access token.
    c.OAuthAdditionalQueryStringParams(
        new Dictionary<string, string>
        {
            {
                "Resource",
                builder.Configuration["AzureAd:ClientId"]
                    ?? throw new ArgumentException("No Azure Ad ClientId")
            },
        }
    );
    c.OAuthUsePkce();
});

var option = new RewriteOptions();
option.AddRedirect("^$", "swagger");
app.UseRewriter(option);

string[] allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
app.UseCors(corsBuilder =>
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

app.MapHub<SignalRHub>(
    "/hub",
    options =>
    {
        options.Transports =
            HttpTransportType.WebSockets
            | HttpTransportType.ServerSentEvents
            | HttpTransportType.LongPolling;
    }
);

app.MapControllers();

app.Run();

#pragma warning disable CA1050
public partial class Program { }
#pragma warning restore CA1050
