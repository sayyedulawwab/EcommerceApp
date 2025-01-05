using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories;
using Ecommerce.Application.Categories.GetCategoryById;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories.GetCategory;
[Route("api/categories")]
[ApiController]
public class GetCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public GetCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        Result<CategoryResponse> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
