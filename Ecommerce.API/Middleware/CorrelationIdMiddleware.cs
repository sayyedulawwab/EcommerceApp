namespace Ecommerce.API.Middleware;

/// <summary>
/// Gives every request a correlation ID, puts it on every log written while the request runs,
/// and returns it in the response so a client can quote it when reporting a problem.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        // Reuse the caller's ID when it sends one, so a request can be followed across services.
        string correlationId = context.Request.Headers[HeaderName] is [{ Length: > 0 } fromCaller]
            ? fromCaller
            : context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // The scope must stay open until the rest of the pipeline has finished, hence the await inside it.
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
