using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories;
using Ecommerce.Application.Categories.GetCategories;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories.GetCategories;
[Route("api/categories")]
[ApiController]
public class GetCategoriesController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetCategoriesQuery();

        Result<IReadOnlyList<CategoryResponse>> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
