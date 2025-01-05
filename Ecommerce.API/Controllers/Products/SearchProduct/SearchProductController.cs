﻿using Ecommerce.Application.Products.SearchProduct;
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
public class SearchProductController : ControllerBase
{
    private readonly ISender _sender;

    public SearchProductController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> SearchProduct([FromQuery] SearchProductRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.ProductCategoryId, request.MinPrice, request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn, request.SortOrder);

        Result<PagedList<ProductResponse>> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
