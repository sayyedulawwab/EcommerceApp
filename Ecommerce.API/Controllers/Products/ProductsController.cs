﻿using Ecommerce.API.Controllers.ProductCategories;
using Ecommerce.Application.ProductCategories.AddProductCategory;
using Ecommerce.Application.ProductCategories.DeleteProductCategory;
using Ecommerce.Application.ProductCategories.EditProductCategory;
using Ecommerce.Application.ProductCategories.GetAllProductCategories;
using Ecommerce.Application.ProductCategories.GetProductCategoryById;
using Ecommerce.Application.Products.AddProduct;
using Ecommerce.Application.Products.DeleteProduct;
using Ecommerce.Application.Products.EditProduct;
using Ecommerce.Application.Products.GetProductById;
using Ecommerce.Application.Products.SearchProduct;
using Ecommerce.Domain.ProductCategories;
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


    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        var result = await _sender.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(request.name, request.description, request.priceCurrency, request.priceAmount, request.quantity, request.productCategoryId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(Guid id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.name, request.description, request.priceCurrency, request.priceAmount, request.quantity, request.productCategoryId);

        var result = await _sender.Send(command, cancellationToken);

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

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }
}
