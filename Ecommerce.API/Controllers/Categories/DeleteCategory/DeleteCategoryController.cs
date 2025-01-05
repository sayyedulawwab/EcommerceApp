using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories.DeleteCategory;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories.DeleteCategory;
[Route("api/categories")]
[ApiController]
public class DeleteCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
