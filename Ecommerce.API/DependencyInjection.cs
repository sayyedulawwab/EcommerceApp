using Ecommerce.API.Infrastructure;
using Ecommerce.Infrastructure.Auth;
using Microsoft.OpenApi;

namespace Ecommerce.API;

public static class DependencyInjection
{
    private const string AccessTokenCookieScheme = "AccessTokenCookie";

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            // Documents the access token cookie. Swagger UI can't set an HttpOnly cookie itself: call
            // /api/v1/auth/login from the UI and the browser stores and sends the cookies.
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[AccessTokenCookieScheme] = new OpenApiSecurityScheme
            {
                In = ParameterLocation.Cookie,
                Description = "Set by POST /api/v1/auth/login as an HttpOnly cookie",
                Name = AuthCookieNames.AccessToken,
                Type = SecuritySchemeType.ApiKey
            };

            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(AccessTokenCookieScheme, document)] = []
                }
            ];

            return Task.CompletedTask;
        }));

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddTrustedForwardedHeaders(configuration);
        services.AddAuthRateLimiting();

        services.AddExceptionHandler<ExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
