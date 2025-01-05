using Ecommerce.Application.Products.SearchProduct;
using Ecommerce.Application.Products;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.API.Extensions;

namespace Ecommerce.API.Controllers.Products.SearchProduct;
[Route("api/products")]
[ApiController]
public class SearchProductController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SearchProduct([FromQuery] SearchProductRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProductQuery(request.CategoryId, request.MinPrice, request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn, request.SortOrder);

        Result<PagedList<ProductResponse>> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
