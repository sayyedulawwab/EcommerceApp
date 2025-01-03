using System.Security.Claims;
using Ecommerce.Application.Reviews.AddReview;
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

        Domain.Abstractions.Result<Guid> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Created(string.Empty, result.Value);
    }

}
