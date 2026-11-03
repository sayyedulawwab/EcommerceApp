using System.Net;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Extensions;

public static class ErrorMappingExtensions
{
    // Every failure, from a Result or from an exception, gets the same RFC 9457 body.
    // "code" is the stable error name a client can match on; "detail" is for people.
    public static IActionResult ToActionResult(this Error error)
    {
        var problemDetails = error.ToProblemDetails();

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static ProblemDetails ToProblemDetails(this Error error)
    {
        (int status, string title, string type) = error.Code switch
        {
            HttpStatusCode.BadRequest => (StatusCodes.Status400BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            HttpStatusCode.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            HttpStatusCode.NotFound => (StatusCodes.Status404NotFound, "Not Found", "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            HttpStatusCode.Conflict => (StatusCodes.Status409Conflict, "Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => (StatusCodes.Status500InternalServerError, "Server failure", "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = error.Description
        };

        problemDetails.Extensions["code"] = error.Name;

        if (error is ValidationError validationError)
        {
            problemDetails.Extensions["errors"] = validationError.Errors
                .Select(e => new { code = e.Name, detail = e.Description })
                .ToArray();
        }

        return problemDetails;
    }
}
