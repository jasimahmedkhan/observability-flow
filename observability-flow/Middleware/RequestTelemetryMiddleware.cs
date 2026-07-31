using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using ObservabilityFlow.Telemetry;

namespace ObservabilityFlow.Middleware;

public sealed class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var statusCode = context.Response.StatusCode;

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["trace_id"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
            ["span_id"] = Activity.Current?.SpanId.ToString() ?? string.Empty,
        });

        try
        {
            logger.LogInformation(
                "Handling {RequestMethod} {RequestPath}",
                context.Request.Method,
                context.Request.Path);

            await next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception exception)
        {
            statusCode = StatusCodes.Status500InternalServerError;
            logger.LogError(
                exception,
                "Unhandled error while processing {RequestMethod} {RequestPath}",
                context.Request.Method,
                context.Request.Path);
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var normalizedRoute = context.GetEndpoint() is RouteEndpoint routeEndpoint
                ? routeEndpoint.RoutePattern.RawText
                : null;
            var tags = new TagList
            {
                { "http.request.method", context.Request.Method },
                { "http.response.status_code", statusCode },
                { "http.route", normalizedRoute ?? "unmatched" },
            };

            CheckoutTelemetry.RequestCounter.Add(1, tags);
            CheckoutTelemetry.RequestDuration.Record(elapsed.TotalMilliseconds, tags);

            logger.LogInformation(
                "Completed {RequestMethod} {RequestPath} with {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                elapsed.TotalMilliseconds);
        }
    }
}
