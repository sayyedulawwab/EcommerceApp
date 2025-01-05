using Ecommerce.API.Extensions;
using Ecommerce.Application.ProductCategories.AddProductCategory;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.ProductCategories.AddCategory;
[Route("api/categories")]
[ApiController]
public class AddCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public AddCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCategoryCommand(request.Name, request.Code);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}
