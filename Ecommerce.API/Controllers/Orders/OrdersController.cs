using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Abstractions.Paging;
using Ecommerce.Application.Orders.CancelOrder;
using Ecommerce.Application.Orders.PlaceOrder;
using Ecommerce.Application.Orders.SearchOrders;
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

        var command = new PlaceOrderCommand(userId, orderItems, request.CouponCode);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, ICommandHandler<CancelOrderCommand> handler, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return Unauthorized("User ID claim is missing.");
        }
        var userId = Guid.Parse(userIdClaim.Value);

        var command = new CancelOrderCommand(userId, id);

        Result result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> SearchOrders([FromQuery] SearchOrdersRequest request, IQueryHandler<SearchOrdersQuery, PagedList<OrderResponse>> handler, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return Unauthorized("User ID claim is missing.");
        }
        var userId = Guid.Parse(userIdClaim.Value);

        var query = new SearchOrdersQuery(userId, request.Status, request.From?.UtcDateTime, request.To?.UtcDateTime, request.Page, request.PageSize, request.SortColumn, request.SortOrder);

        Result<PagedList<OrderResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
