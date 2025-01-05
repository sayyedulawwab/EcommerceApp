using Ecommerce.API.Extensions;
using Ecommerce.Application.Products.EditProduct;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products.EditProduct;
[Route("api/products")]
[ApiController]
[Authorize]
public class EditProductController(ISender sender) : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(Guid id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.CategoryId);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
