using System.Diagnostics.Metrics;

namespace ObservabilityFlow.Telemetry;

internal static class CheckoutTelemetry
{
    internal const string MeterName = "CheckoutService";

    internal static readonly Meter Meter = new(MeterName, "1.0.0");

    internal static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "checkout.http.server.requests",
        unit: "{request}",
        description: "Number of HTTP requests handled by checkout-service.");

    internal static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "checkout.http.server.request.duration",
        unit: "ms",
        description: "Checkout-service HTTP request latency.");
}
