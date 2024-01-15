using Ecommerce.Application.ProductCategories.AddProductCategory;
using Ecommerce.Application.ProductCategories.GetAllProductCategories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.ProductCategories;

[Route("api/productcategories")]
[ApiController]
public class ProductCategoriesController : ControllerBase
{
    private readonly ISender _sender;
    public ProductCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllProductCategoriesQuery();

        var result = await _sender.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductCategory(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductCategoryByIdQuery(id);

        var result = await _sender.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddProductCategory(AddProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCategoryCommand(request.name, request.code);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction("GetProductCategory", new { id = result.Value });
    }
}
