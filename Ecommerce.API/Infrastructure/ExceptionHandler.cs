using System.Net;
using Ecommerce.API.Extensions;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Diagnostics;

namespace Ecommerce.API.Infrastructure;

internal sealed class ExceptionHandler(ILogger<ExceptionHandler> logger)
    : IExceptionHandler
{
    // The exception message can contain SQL, table names or stack details, so the client gets a fixed message.
    private static readonly Error ServerFailure = new(
        "Server.Failure",
        "An unexpected error occurred",
        HttpStatusCode.InternalServerError);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = ServerFailure.ToProblemDetails();

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
