using System.Reflection;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.DomainEvents;
using Ecommerce.Infrastructure.Emails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // One data source for the app's lifetime: it owns the connection pool, and Dapper queries
        // open their connections from it. Created on first use, for the same reason as above.
        services.AddSingleton(_ => NpgsqlDataSource.Create(GetConnectionString()));
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
