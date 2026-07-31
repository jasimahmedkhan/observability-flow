namespace ObservabilityFlow.Contracts.Checkout;

public sealed record CheckoutResponse(Guid OrderId, string Status);
