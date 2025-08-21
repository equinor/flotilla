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
                    .SetErrorStatusOnException(true)
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
        var openTelemetryProtocolSetting = builder.Configuration[
            "OpenTelemetry:OtelExporterOtlpProtocol"
        ];

        Console.WriteLine($"OpenTelemetry endpoint: {openTelemetryEndpoint}");
        Console.WriteLine(
            $"OpenTelemetry protocol: {openTelemetryProtocolSetting ?? "Not set (using default HttpProtobuf)"}"
        );

        if (!string.IsNullOrWhiteSpace(openTelemetryEndpoint))
        {
            var uri = new Uri(openTelemetryEndpoint);
            var protocol = OtlpExportProtocol.HttpProtobuf; // Default

            if (!string.IsNullOrWhiteSpace(openTelemetryProtocolSetting))
            {
                switch (openTelemetryProtocolSetting.ToLower())
                {
                    case "grpc":
                        protocol = OtlpExportProtocol.Grpc;
                        Console.WriteLine("Using gRPC protocol for OpenTelemetry export");
                        break;
                    case "httpprotobuf":
                        Console.WriteLine("Using HTTP/Protobuf protocol for OpenTelemetry export");
                        break;
                    default:
                        Console.WriteLine(
                            $"Unknown protocol '{openTelemetryProtocolSetting}', defaulting to HTTP/Protobuf"
                        );
                        break;
                }
            }
            else
            {
                Console.WriteLine("Using default HTTP/Protobuf protocol for OpenTelemetry export");
            }

            otel.UseOtlpExporter(protocol, uri);
        }

        return builder;
    }
}
