using Microsoft.AspNetCore.Mvc;
using ObservabilityFlow.Application.Checkout;
using ObservabilityFlow.Contracts.Checkout;

namespace ObservabilityFlow.Controllers;

[ApiController]
[Route("checkout")]
public sealed class CheckoutController(ICheckoutService checkoutService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CheckoutResponse>(StatusCodes.Status202Accepted)]
    public ActionResult<CheckoutResponse> Checkout(CheckoutRequest request)
    {
        var response = checkoutService.Checkout(request);

        return Accepted($"/checkout/{response.OrderId}", response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<CheckoutResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<CheckoutResponse>> GetAll()
    {
        return Ok(checkoutService.GetAll());
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType<CheckoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CheckoutResponse> GetById(Guid orderId)
    {
        var checkout = checkoutService.GetById(orderId);

        return checkout is null
            ? NotFound()
            : Ok(checkout);
    }

    [HttpDelete("{orderId:guid}")]
    [ProducesResponseType<CheckoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CheckoutResponse> Cancel(Guid orderId)
    {
        var checkout = checkoutService.Cancel(orderId);

        return checkout is null
            ? NotFound()
            : Ok(checkout);
    }
}
