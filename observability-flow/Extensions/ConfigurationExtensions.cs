namespace ObservabilityFlow.Extensions;

internal static class ConfigurationExtensions
{
    private const string DefaultServiceName = "checkout-service";
    private const string DefaultOtlpEndpoint = "http://otel-collector:4317";

    internal static string GetOpenTelemetryServiceName(this IConfiguration configuration)
    {
        return configuration["OTEL_SERVICE_NAME"] ?? DefaultServiceName;
    }

    internal static Uri GetOtlpEndpoint(this IConfiguration configuration)
    {
        var configuredEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? DefaultOtlpEndpoint;

        if (Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var endpoint))
        {
            return endpoint;
        }

        throw new InvalidOperationException($"OTEL_EXPORTER_OTLP_ENDPOINT must be an absolute URI, but was '{configuredEndpoint}'.");
    }
}
