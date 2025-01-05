using System.Security.Claims;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Reviews.AddReview;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Reviews;
[Route("api/reviews")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly ISender _sender;
    public ReviewsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> GiveReview([FromBody] GiveReviewRequest request, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim?.Value is null)
        {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim.Value);

        var command = new AddReviewCommand(request.ProductId, userId, request.Rating, request.Comment);

        Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, result.Value);
    }

}
