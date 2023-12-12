using Ecommerce.Application.Services.Auth.Commands;
using Ecommerce.Application.Services.Auth.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthCommandService, AuthCommandService>();
            services.AddScoped<IAuthQueryService, AuthQueryService>();
            return services;
        }
    }
}
