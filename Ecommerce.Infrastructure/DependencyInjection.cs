using Dapper;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Application.Products;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Clock;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.DomainEvents;
using Ecommerce.Infrastructure.Emails;
using Ecommerce.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        services.AddStackExchangeRedisCache(redisOptions =>
        {

            string? connection = configuration.GetConnectionString("Redis");

            redisOptions.Configuration = connection;
        });

        services.AddSingleton<ICacheService, CacheService>();

        AddPersistence(services, configuration);
        AddAuthentication(services);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        // Read when first resolved, not at registration: build-time OpenAPI generation boots the app
        // without any environment config and never touches the database.
        string GetConnectionString() => configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Database' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(GetConnectionString()).UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IProductQueryRepository, ProductQueryRepository>();

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(GetConnectionString()));

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.ConfigureOptions<JwtOptionsSetup>();

        services.ConfigureOptions<JwtBearerOptionsSetup>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IAuthService, AuthService>();
    }
}
