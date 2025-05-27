using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Orders.GetAllOrders;
using Ecommerce.Application.Orders.PlaceOrder;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers.Orders;
[Route("api/v{v:apiVersion}/orders")]
[ApiController]
[Authorize]
public class OrdersController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, ICommandHandler<PlaceOrderCommand, Guid> handler, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return Unauthorized("User ID claim is missing.");
        }
        var userId = Guid.Parse(userIdClaim.Value);

        var orderItems = request.OrderItems.Select(item =>
            new OrderStockItem(item.ProductId, item.Quantity))
            .ToList();

        var command = new PlaceOrderCommand(userId, orderItems);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, result.Value);
    }

    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> GetOrders(IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        Result<IReadOnlyList<OrderResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
