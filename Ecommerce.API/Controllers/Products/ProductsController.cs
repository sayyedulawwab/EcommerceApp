using Ecommerce.Application.ProductCategories.GetAllProductCategories;
using Ecommerce.Application.Products.SearchProduct;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products;

[Route("api/products")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Search(string name, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(name);

        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
