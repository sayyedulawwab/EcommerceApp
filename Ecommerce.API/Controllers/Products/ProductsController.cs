using Ecommerce.Application.Products;
using Ecommerce.Application.Products.AddProduct;
using Ecommerce.Application.Products.DeleteProduct;
using Ecommerce.Application.Products.EditProduct;
using Ecommerce.Application.Products.GetProductById;
using Ecommerce.Application.Products.SearchProduct;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products;

[Route("api/products")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.ProductCategoryId, request.MinPrice, request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn, request.SortOrder);

        Result<PagedList<ProductResponse>> result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await _sender.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.ProductCategoryId);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(Guid id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.ProductCategoryId);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }
}
