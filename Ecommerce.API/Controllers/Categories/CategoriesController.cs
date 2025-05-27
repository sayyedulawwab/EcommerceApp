using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;
using Ecommerce.Application.Categories.AddCategory;
using Ecommerce.Application.Categories.DeleteCategory;
using Ecommerce.Application.Categories.EditCategory;
using Ecommerce.Application.Categories.GetCategories;
using Ecommerce.Application.Categories.GetCategoryById;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Categories;
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
[Authorize]
public class CategoriesController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, ICommandHandler<AddCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.Name, request.Code);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }

    [AllowAnonymous]
    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> GetCategories(IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new GetCategoriesQuery();

        Result<IReadOnlyList<CategoryResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [AllowAnonymous]
    [MapToApiVersion(1)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(Guid id, IQueryHandler<GetCategoryByIdQuery, CategoryResponse> handler, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        Result<CategoryResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(Guid id, EditCategoryRequest request, ICommandHandler<EditCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.Name, request.Code);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id, ICommandHandler<DeleteCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
