using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories.AddCategory;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories.AddCategory;
[Route("api/categories")]
[ApiController]
public class AddCategoryController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.Name, request.Code);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}
