using Ecommerce.API.Middleware;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.MapOpenApi();

        // The document comes from Microsoft.AspNetCore.OpenApi; Swagger UI only renders it.
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Ecommerce API V1"));

        return app;
    }
}
