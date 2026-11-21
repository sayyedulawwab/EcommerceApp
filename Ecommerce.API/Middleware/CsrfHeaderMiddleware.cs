using System.Net;
using Ecommerce.API.Extensions;
using Ecommerce.Domain.Abstractions;

namespace Ecommerce.API.Middleware;

/// <summary>
/// The second defense against CSRF, behind the SameSite=Strict auth cookies. Every request that can change
/// state must carry "X-CSRF: 1". A form or a link on another site can't add a custom header, and a script on
/// another origin can only add one after a CORS preflight, which this API never allows. That also covers
/// sibling subdomains, which count as the same site, so SameSite alone doesn't stop them.
/// </summary>
internal sealed class CsrfHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-CSRF";

    private static readonly Error MissingHeader = new(
        "Request.MissingCsrfHeader",
        $"Requests that change data must send the {HeaderName}: 1 header",
        HttpStatusCode.Forbidden);

    public async Task InvokeAsync(HttpContext context)
    {
        string method = context.Request.Method;

        bool isSafe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);

        if (!isSafe && context.Request.Headers[HeaderName] != "1")
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(
                MissingHeader.ToProblemDetails(),
                options: null,
                contentType: "application/problem+json",
                context.RequestAborted);

            return;
        }

        await next(context);
    }
}
