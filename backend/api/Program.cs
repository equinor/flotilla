using System.Text.Json.Serialization;
using Api.Configurations;
using Api.Context;
using Api.Scheduler;
using Api.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAzureClients(azureBuilder =>
{
    azureBuilder.AddSecretClient(builder.Configuration.GetSection("KeyVault"));

    azureBuilder.UseCredential(new DefaultAzureCredential());
});

builder.Services.AddDbContext<FlotillaDbContext>(options => options.UseInMemoryDatabase("flotilla"));

builder.Services.AddSingleton<DefaultAzureCredential>();
builder.Services.AddSingleton<KeyVaultService>();

builder.Services.AddScoped<RobotService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IsarService>();
builder.Services.AddScoped<EchoService>();

builder.Services.AddHostedService<EventScheduler>();

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(builder.Configuration.GetSection("Authorization")["Roles"])
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
        // The following parameter represents the "audience" of the access token.
        c.OAuthAdditionalQueryStringParams(new Dictionary<string, string> { { "Resource", builder.Configuration["AzureAd:ClientId"] } });
        c.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
