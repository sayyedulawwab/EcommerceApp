using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Categories;
using Ecommerce.Application.Categories.AddCategory;
using Ecommerce.Application.Categories.DeleteCategory;
using Ecommerce.Application.Categories.EditCategory;
using Ecommerce.Application.Categories.GetCategories;
using Ecommerce.Application.Categories.GetCategoryById;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories;
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
[Authorize]
public class CategoriesController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.Name, request.Code);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }

    [AllowAnonymous]
    [MapToApiVersion(1)]
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

    [AllowAnonymous]
    [MapToApiVersion(1)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        Result<CategoryResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(Guid id, EditCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.Name, request.Code);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
