using Microsoft.Extensions.Primitives;

namespace Ecommerce.API.Middleware;

public class RequestContextLoggingMiddleware(RequestDelegate next, ILogger<RequestContextLoggingMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public async Task Invoke(HttpContext context)
    {
        string correlationId = GetCorrelationId(context);

        // Every log written during the request carries the CorrelationId. The scope stays open until the request completes.
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next.Invoke(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName, out StringValues correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
