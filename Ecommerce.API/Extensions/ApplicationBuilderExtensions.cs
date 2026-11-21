using Ecommerce.API.Middleware;

namespace Ecommerce.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.MapOpenApi();

        // The document comes from Microsoft.AspNetCore.OpenApi; Swagger UI only renders it.
        // Swagger UI runs on the API's origin, so the browser sends the auth cookies after a login from the UI.
        // The interceptor adds the CSRF header that every state-changing request needs.
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Ecommerce API V1");
            options.UseRequestInterceptor($"(request) => {{ request.headers['{CsrfHeaderMiddleware.HeaderName}'] = '1'; return request; }}");
        });

        return app;
    }
}
