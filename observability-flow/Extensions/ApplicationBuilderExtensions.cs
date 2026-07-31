using ObservabilityFlow.Middleware;

namespace ObservabilityFlow.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRequestTelemetry(this IApplicationBuilder application)
    {
        return application.UseMiddleware<RequestTelemetryMiddleware>();
    }
}
