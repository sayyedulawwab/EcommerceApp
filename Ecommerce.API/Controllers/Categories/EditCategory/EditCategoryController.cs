using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories.EditCategory;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories.EditCategory;
[Route("api/categories")]
[ApiController]
public class EditCategoryController(ISender sender) : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(Guid id, EditCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.Name, request.Code);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
