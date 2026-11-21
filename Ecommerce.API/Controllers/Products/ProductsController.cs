using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Products;
using Ecommerce.Application.Products.AddProduct;
using Ecommerce.Application.Products.DeleteProduct;
using Ecommerce.Application.Products.EditProduct;
using Ecommerce.Application.Products.GetProductById;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products;

[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class ProductsController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, ICommandHandler<AddProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.CategoryId);

        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(Guid id, EditProductRequest request, ICommandHandler<EditProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.CategoryId);

        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id, IQueryHandler<GetProductByIdQuery, ProductResponse> handler, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id, ICommandHandler<DeleteProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
