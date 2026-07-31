using System.ComponentModel.DataAnnotations;

namespace ObservabilityFlow.Contracts.Checkout;

public sealed record CheckoutRequest(
    [Required, MinLength(1)] string CartId,
    [Range(1, int.MaxValue)] int ItemCount);