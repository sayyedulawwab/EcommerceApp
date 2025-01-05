using Ecommerce.Application.Reviews.AddReview;
using Ecommerce.Domain.Abstractions;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.API.Extensions;

namespace Ecommerce.API.Controllers.Reviews.GiveReview;
[Route("api/reviews")]
[ApiController]
[Authorize]
public class GiveReviewController : ControllerBase
{
    private readonly ISender _sender;

    public GiveReviewController(ISender sender)
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
