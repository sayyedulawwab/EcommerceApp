using Ecommerce.Application.ProductCategories.GetAllProductCategories;
using Ecommerce.Application.ProductCategories;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.API.Extensions;

namespace Ecommerce.API.Controllers.ProductCategories.GetCategories;
[Route("api/categories")]
[ApiController]
public class GetCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public GetCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllProductCategoriesQuery();

        Result<IReadOnlyList<ProductCategoryResponse>> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
