using Ecommerce.Application.Products.GetProductById;
using Ecommerce.Application.Products;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.API.Extensions;

namespace Ecommerce.API.Controllers.Products.GetProduct;
[Route("api/products")]
[ApiController]
public class GetProductController(ISender sender) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
