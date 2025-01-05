using Ecommerce.Application.Orders.PlaceOrder;
using Ecommerce.Domain.Abstractions;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.API.Extensions;

namespace Ecommerce.API.Controllers.Orders.PlaceOrder;
[Route("api/orders")]
[ApiController]
[Authorize]
public class PlaceOrderController : ControllerBase
{
    private readonly ISender _sender;
    public PlaceOrderController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return Unauthorized("User ID claim is missing.");
        }
        var userId = Guid.Parse(userIdClaim.Value);

        var orderItems = request.OrderItems.Select(item =>
            new PlaceOrderProductCommand(item.ProductId, item.Quantity))
            .ToList();

        var command = new PlaceOrderCommand(userId, orderItems);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, result.Value);
    }
}
