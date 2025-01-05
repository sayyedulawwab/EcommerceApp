using Ecommerce.Application.ProductCategories.GetProductCategoryById;
using Ecommerce.Application.ProductCategories;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.API.Extensions;

namespace Ecommerce.API.Controllers.ProductCategories.GetCategory;
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
        var query = new GetProductCategoryByIdQuery(id);

        Result<ProductCategoryResponse> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
