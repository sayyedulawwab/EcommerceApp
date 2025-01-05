using Ecommerce.API.Extensions;
using Ecommerce.Application.Products.DeleteProduct;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products.DeleteProduct;
[Route("api/products")]
[ApiController]
[Authorize]
public class DeleteProductController(ISender sender) : ControllerBase
{
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
