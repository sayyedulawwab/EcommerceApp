using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Reviews.AddReview;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers.Reviews;
[Route("api/v{v:apiVersion}/reviews")]
[ApiController]
[Authorize]
public class ReviewsController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
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

        Result<Guid> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, result.Value);
    }
}
