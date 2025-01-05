using Ecommerce.API.Extensions;
using Ecommerce.Application.Orders.GetAllOrders;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Orders.GetOrders;
[Route("api/orders")]
[ApiController]
[Authorize]
public class GetOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        Result<IReadOnlyList<OrderResponse>> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
