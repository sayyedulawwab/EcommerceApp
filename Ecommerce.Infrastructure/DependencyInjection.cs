using System.Reflection;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Application.Products;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.Infrastructure.Clock;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.DomainEvents;
using Ecommerce.Infrastructure.Emails;
using Ecommerce.Infrastructure.Health;
using Ecommerce.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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

            // ConfigurationOptions.Parse throws on an empty string, so without a connection string the options stay unset.
            if (string.IsNullOrWhiteSpace(connection))
            {
                return;
            }

            // Fail fast while Redis is down: a command fails at once instead of waiting in the client's
            // backlog until its 5 s timeout, so HybridCache falls back to the database without slowing
            // the request. The cost: commands sent during a short reconnect fail too, and go to the database.
            var redis = ConfigurationOptions.Parse(connection);
            redis.BacklogPolicy = BacklogPolicy.FailFast;
            redisOptions.ConfigurationOptions = redis;
        });

        // HybridCache keeps results in memory (L1) and in Redis (L2, the IDistributedCache above).
        // These lifetimes are starting values, not measured ones.
        services.AddHybridCache(hybridOptions => hybridOptions.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(5),
            LocalCacheExpiration = TimeSpan.FromMinutes(1)
        });

        AddPersistence(services, configuration);
        AddAuthentication(services);

        // Status only: the endpoint is anonymous, so it never reports details or exceptions.
        // Without Redis the API still answers correctly, from the database (the Redis client fails fast, above):
        // HybridCache logs failed Redis reads and writes and runs the query, and ProductCache
        // logs a failed clear. So a Redis failure is Degraded (still 200, the container keeps its traffic), not
        // Unhealthy (503, Traefik drops every container at once).
        // The timeout keeps a Redis that never answers inside the compose probe's 5 s limit.
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, timeout: TimeSpan.FromSeconds(2));

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
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.ConfigureOptions<JwtOptionsSetup>();

        // Fail at startup, not on the first login, when the key is missing or too short.
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        OptionsBuilder<JwtOptions> jwtOptionsBuilder = services.AddOptions<JwtOptions>();

        // The build-time OpenAPI step boots the app without any configuration, so it is the one host that skips the check.
        if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
        {
            jwtOptionsBuilder.ValidateOnStart();
        }

        services.ConfigureOptions<JwtBearerOptionsSetup>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddOptions<PasswordHashingOptions>()
            .BindConfiguration("PasswordHashing")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAuthService, AuthService>();
    }
}
