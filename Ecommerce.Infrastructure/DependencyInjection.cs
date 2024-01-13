using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Email;
using Ecommerce.Infrastructure.Clock;
using Ecommerce.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}
