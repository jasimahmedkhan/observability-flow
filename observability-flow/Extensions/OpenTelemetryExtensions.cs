using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ObservabilityFlow.Telemetry;

namespace ObservabilityFlow.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddCheckoutOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration.GetOpenTelemetryServiceName();
        var otlpEndpoint = configuration.GetOtlpEndpoint();

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = otlpEndpoint;
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .SetExemplarFilter(ExemplarFilterType.TraceBased)
                .AddMeter(CheckoutTelemetry.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = otlpEndpoint;
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                }));

        return services;
    }
}
