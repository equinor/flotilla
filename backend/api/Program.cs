using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Database.Context;
using Api.EventHandlers;
using Api.Mqtt;
using Api.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureEnvironmentVariables();

SqliteConnection connectionToInMemorySqlite = null!;
string sqlConnectionString = builder.Configuration.GetSection("Database").GetValue<string>("ConnectionString");
if (string.IsNullOrEmpty(sqlConnectionString))
{
    var dbBuilder = new DbContextOptionsBuilder<FlotillaDbContext>();
    sqlConnectionString = new SqliteConnectionStringBuilder { DataSource = "file::memory:", Cache = SqliteCacheMode.Shared }.ToString();

    // In-memory sqlite requires an open connection throughout the whole lifetime of the database
    connectionToInMemorySqlite = new SqliteConnection(sqlConnectionString);
    connectionToInMemorySqlite.Open();
    dbBuilder.UseSqlite(connectionToInMemorySqlite);

    using var context = new FlotillaDbContext(dbBuilder.Options);
    context.Database.EnsureCreated();
    InitDb.PopulateDb(context);
}

if (connectionToInMemorySqlite == null)
{
    // Setting splitting behavior explicitly to avoid warning
    builder.Services.AddDbContext<FlotillaDbContext>(
        options => options.UseSqlServer(sqlConnectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
    );
}
else
{
    // Setting splitting behavior explicitly to avoid warning
    builder.Services.AddDbContext<FlotillaDbContext>(
        options => options.UseSqlite(sqlConnectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
    );
}

builder.Services.AddScoped<RobotService>();
builder.Services.AddScoped<ScheduledMissionService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IsarService>();
builder.Services.AddScoped<EchoService>();

builder.Services.AddHostedService<MqttEventHandler>();
builder.Services.AddHostedService<MqttService>();
builder.Services.AddHostedService<ScheduledMissionEventHandler>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches()
    .AddDownstreamWebApi(EchoService.ServiceName, builder.Configuration.GetSection("Echo"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(builder.Configuration.GetSection("Authorization")["Roles"])
        .Build();
});

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
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
    });
}
app.UseCors(
    corsBuilder =>
        corsBuilder
            .WithOrigins(builder.Configuration.GetValue<string>("AllowedOrigins"))
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
