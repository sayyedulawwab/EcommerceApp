using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories.DeleteCategory;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories.DeleteCategory;
[Route("api/categories")]
[ApiController]
public class DeleteCategoryController(ISender sender) : ControllerBase
{
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
