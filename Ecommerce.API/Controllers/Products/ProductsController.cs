using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Products.EditProduct;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products;

[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class ProductsController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(Guid id, EditProductRequest request, ICommandHandler<EditProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.CategoryId);

        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
