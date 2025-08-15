using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Api.Configurations;

public static class TelemetryConfigurations
{
    public static WebApplicationBuilder AddCustomOpenTelemetry(
        this WebApplicationBuilder builder,
        ActivitySource activitySource,
        Meter meter
    )
    {
        var applicationName = builder.Configuration["AppName"] ?? "FlotillaBackend";

        // Logging
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        // Tracing & Metrics pipeline
        var otel = builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(applicationName))
            .WithTracing(t =>
            {
                t.SetSampler(new AlwaysOnSampler())
                    .SetResourceBuilder(
                        ResourceBuilder
                            .CreateDefault()
                            .AddService(applicationName, serviceVersion: "0.0.1")
                    )
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddSource(activitySource.Name);
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                    .AddMeter(meter.Name)
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();
            });

        // Conditionally connect to Azure Monitor
        var azureMonitorExportEnabled =
            builder.Configuration.GetValue<bool?>("OpenTelemetry:AzureMonitorExportEnabled")
            ?? false;

        if (azureMonitorExportEnabled)
        {
            var applicationInsightsConnectionString = builder.Configuration[
                "ApplicationInsights:ConnectionString"
            ];

            if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
            {
                builder
                    .Services.AddOpenTelemetry()
                    .UseAzureMonitor(o =>
                    {
                        o.ConnectionString = applicationInsightsConnectionString;
                    });
            }
        }

        // Connect to OpenTelemetry OTLP exporter if endpoint is provided, used for local aspire dashboard
        var openTelemetryEndpoint = builder.Configuration["OpenTelemetry:OtelExporterOtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(openTelemetryEndpoint))
        {
            var uri = new Uri(openTelemetryEndpoint);
            var protocol = OtlpExportProtocol.Grpc;

            otel.UseOtlpExporter(protocol, uri);
        }

        return builder;
    }
}
