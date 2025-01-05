using System.Security.Claims;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Orders.GetAllOrders;
using Ecommerce.Application.Orders.PlaceOrder;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Orders;
[Route("api/orders")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        Result<IReadOnlyList<OrderResponse>> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
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
