using System.Net;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Exceptions;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Infrastructure;

internal sealed class ExceptionHandler(ILogger<ExceptionHandler> logger)
    : IExceptionHandler
{
    // Someone else changed the same data first. The client can reload and retry.
    private static readonly Error ConcurrencyConflict = new(
        "Concurrency.Conflict",
        "The data was changed by another request. Reload it and try again",
        HttpStatusCode.Conflict);

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
        Error error;

        if (exception is ConcurrencyException)
        {
            logger.LogWarning(exception, "Concurrency conflict");

            error = ConcurrencyConflict;
        }
        else
        {
            logger.LogError(exception, "Unhandled exception occurred");

            error = ServerFailure;
        }

        var problemDetails = error.ToProblemDetails();

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
