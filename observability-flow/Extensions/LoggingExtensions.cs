using System.Diagnostics;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace ObservabilityFlow.Extensions;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddCheckoutLogging(this ILoggingBuilder logging, IConfiguration configuration)
    {
        var serviceName = configuration.GetOpenTelemetryServiceName();
        var otlpEndpoint = configuration.GetOtlpEndpoint();

        logging.ClearProviders();
        logging.Configure(options =>
        {
            options.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId |
                ActivityTrackingOptions.SpanId |
                ActivityTrackingOptions.ParentId;
        });
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "O";
            options.UseUtcTimestamp = true;
            options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = false,
            };
        });
        logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = otlpEndpoint;
                exporter.Protocol = OtlpExportProtocol.Grpc;
            });
        });

        return logging;
    }
}
