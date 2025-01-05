using Ecommerce.API.Extensions;
using Ecommerce.Application.Products.AddProduct;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Products.AddProduct;
[Route("api/products")]
[ApiController]
[Authorize]
public class AddProductController : ControllerBase
{
    private readonly ISender _sender;

    public AddProductController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(request.Name, request.Description, request.PriceCurrency, request.PriceAmount, request.Quantity, request.CategoryId);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}
