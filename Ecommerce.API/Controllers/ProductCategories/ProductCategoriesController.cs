using Ecommerce.Application.ProductCategories.AddProductCategory;
using Ecommerce.Application.ProductCategories.DeleteProductCategory;
using Ecommerce.Application.ProductCategories.EditProductCategory;
using Ecommerce.Application.ProductCategories.GetAllProductCategories;
using Ecommerce.Application.ProductCategories.GetProductCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.ProductCategories;

[Route("api/productcategories")]
[ApiController]
[Authorize]
public class ProductCategoriesController : ControllerBase
{
    private readonly ISender _sender;
    public ProductCategoriesController(ISender sender)
    {
        _sender = sender;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetProductCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllProductCategoriesQuery();

        var result = await _sender.Send(query, cancellationToken);

        return Ok(result.Value);
    }
    [AllowAnonymous]
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

        return CreatedAtAction(nameof(GetProductCategory), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditProductCategory(Guid id, EditProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCategoryCommand(id, request.name, request.code);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProductCategory(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCategoryCommand(id);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }


}
