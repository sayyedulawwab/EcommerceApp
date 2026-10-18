using System.Net;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Extensions;

public static class ErrorMappingExtensions
{
    public static IActionResult ToActionResult(this Error error)
    {
        return error.Code switch
        {
            HttpStatusCode.BadRequest => new BadRequestObjectResult(error),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(error),
            HttpStatusCode.NotFound => new NotFoundObjectResult(error),
            HttpStatusCode.Conflict => new ConflictObjectResult(error),
            HttpStatusCode.InternalServerError => new ObjectResult(error)
            {
                StatusCode = 500
            },
            _ => new ObjectResult(error)
            {
                StatusCode = 500
            }
        };
    }
}
