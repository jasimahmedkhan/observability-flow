using ObservabilityFlow.Application.Checkout;

namespace ObservabilityFlow.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheckoutApplication(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddHttpClient();
        services.AddSingleton<ICheckoutService, CheckoutService>();

        return services;
    }
}
