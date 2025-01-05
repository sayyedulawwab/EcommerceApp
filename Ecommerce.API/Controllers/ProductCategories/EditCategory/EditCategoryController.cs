using Ecommerce.API.Extensions;
using Ecommerce.Application.ProductCategories.EditProductCategory;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.ProductCategories.EditCategory;
[Route("api/categories")]
[ApiController]
public class EditCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public EditCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(Guid id, EditCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCategoryCommand(id, request.Name, request.Code);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
