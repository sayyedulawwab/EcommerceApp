using Ecommerce.API.Infrastructure;
using Microsoft.OpenApi;

namespace Ecommerce.API;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter a valid JSON web token here",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            };

            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                }
            ];

            return Task.CompletedTask;
        }));

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddExceptionHandler<ExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
