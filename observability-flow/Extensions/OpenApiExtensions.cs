using Microsoft.OpenApi.Models;

namespace ObservabilityFlow.Extensions;

public static class OpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddCheckoutOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "Checkout Service API",
                Version = DocumentName,
                Description = "Sample checkout API instrumented with OpenTelemetry.",
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCheckoutOpenApi(this IApplicationBuilder application)
    {
        application.UseSwagger();
        application.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                $"/swagger/{DocumentName}/swagger.json",
                "Checkout Service API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Checkout Service API";
        });

        return application;
    }
}
