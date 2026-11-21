using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Infrastructure.DomainEvents;
using Ecommerce.Infrastructure.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // .NET's own clock abstraction. Handlers take TimeProvider, so a test can pass a fixed clock instead.
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        AddPersistence(services, configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        string GetConnectionString() => configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Database' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(GetConnectionString()).UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // One data source for the app's lifetime: it owns the connection pool, and Dapper queries
        // open their connections from it. Created on first use, for the same reason as above.
        services.AddSingleton(_ => NpgsqlDataSource.Create(GetConnectionString()));
    }
}
