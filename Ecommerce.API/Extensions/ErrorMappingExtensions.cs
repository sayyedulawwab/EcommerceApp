using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Extensions;

public static class ErrorMappingExtensions
{
    public static IActionResult ToActionResult(this Error error)
    {
        return error.Code switch
        {
            HttpResponseStatusCodes.BadRequest => new BadRequestObjectResult(error),
            HttpResponseStatusCodes.NotFound => new NotFoundObjectResult(error),
            HttpResponseStatusCodes.Conflict => new ConflictObjectResult(error),
            HttpResponseStatusCodes.InternalServerError => new ObjectResult(error)
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
