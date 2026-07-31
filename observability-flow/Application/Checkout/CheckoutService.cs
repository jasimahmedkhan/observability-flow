using System.Collections.Concurrent;
using ObservabilityFlow.Contracts.Checkout;

namespace ObservabilityFlow.Application.Checkout;

internal sealed class CheckoutService(ILogger<CheckoutService> logger) : ICheckoutService
{
    private readonly ConcurrentDictionary<Guid, CheckoutResponse> _checkouts = new();

    public CheckoutResponse Checkout(CheckoutRequest request)
    {
        var orderId = Guid.NewGuid();
        var response = new CheckoutResponse(orderId, "accepted");

        _checkouts[orderId] = response;

        logger.LogInformation(
            "Checkout accepted for cart {CartId} with {ItemCount} items; order {OrderId}",
            request.CartId,
            request.ItemCount,
            orderId);

        return response;
    }

    public IReadOnlyCollection<CheckoutResponse> GetAll()
    {
        return _checkouts.Values
            .OrderBy(checkout => checkout.OrderId)
            .ToArray();
    }

    public CheckoutResponse? GetById(Guid orderId)
    {
        _checkouts.TryGetValue(orderId, out var checkout);
        return checkout;
    }

    public CheckoutResponse? Cancel(Guid orderId)
    {
        while (_checkouts.TryGetValue(orderId, out var checkout))
        {
            if (checkout.Status == "cancelled")
            {
                return checkout;
            }

            var cancelledCheckout = checkout with { Status = "cancelled" };

            if (_checkouts.TryUpdate(orderId, cancelledCheckout, checkout))
            {
                logger.LogInformation("Checkout order {OrderId} was cancelled", orderId);
                return cancelledCheckout;
            }
        }

        return null;
    }
}
