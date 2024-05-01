using Ecommerce.Application.Reviews.AddReview;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim?.Value);


        var command = new AddReviewCommand(request.productId, userId, request.rating, request.comment);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Created(string.Empty, result.Value);
    }

}
