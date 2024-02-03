﻿using Ecommerce.Application.Orders.GetAllOrders;
using Ecommerce.Application.Orders.PlaceOrder;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers.Orders;
[Route("api/orders")]
[ApiController]
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

        var result = await _sender.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var command = new PlaceOrderCommand(userId, request.orderItems);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Created(string.Empty, result.Value);
    }

}
