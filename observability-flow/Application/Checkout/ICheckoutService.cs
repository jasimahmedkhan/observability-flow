using ObservabilityFlow.Contracts.Checkout;

namespace ObservabilityFlow.Application.Checkout;

public interface ICheckoutService
{
    CheckoutResponse Checkout(CheckoutRequest request);

    IReadOnlyCollection<CheckoutResponse> GetAll();

    CheckoutResponse? GetById(Guid orderId);

    CheckoutResponse? Cancel(Guid orderId);
}
