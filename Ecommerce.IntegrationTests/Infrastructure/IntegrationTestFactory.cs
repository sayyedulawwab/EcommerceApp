using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Emails;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Respawn;
using System.Data.Common;
using System.Net.Http.Headers;
using Testcontainers.PostgreSql;

namespace Ecommerce.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    private DatabaseReset _databaseReset = default!;

    private DbConnection _connection = default!;

    public HttpClient Client { get; private set; } = default!;

    public string ConnectionString => _postgres.GetConnectionString();

    public IntegrationTestFactory()
    {
        _postgres = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("ecommerce")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Client = CreateClient();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                TestAuthenticationHandler.AuthenticationScheme);

        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        ApplicationDbContext db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();

        _connection = new NpgsqlConnection(ConnectionString);

        await _connection.OpenAsync();

        _databaseReset = await DatabaseReset.CreateAsync(_connection);
    }

    public Task ResetDatabaseAsync()
    {
        return _databaseReset.ResetAsync(_connection);
    }

    public HttpClient CreateClientForUser(Guid userId)
    {
        HttpClient client = CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                TestAuthenticationHandler.AuthenticationScheme);

        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            userId.ToString());

        return client;
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _postgres.DisposeAsync();

        await base.DisposeAsync();
    }

    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }

    public async Task ExecuteScopeAsync(
        Func<IServiceProvider, Task> action)
    {
        await using AsyncServiceScope scope =
            Services.CreateAsyncScope();

        await action(scope.ServiceProvider);
    }

    public async Task<TResult> ExecuteScopeAsync<TResult>(
        Func<IServiceProvider, Task<TResult>> action)
    {
        await using AsyncServiceScope scope =
            Services.CreateAsyncScope();

        return await action(scope.ServiceProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = ConnectionString,
                    ["ConnectionStrings:Redis"] = "localhost:6379"
                }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(ConnectionString)
                       .UseSnakeCaseNamingConvention());

            services.RemoveAll<ISqlConnectionFactory>();

            services.AddSingleton<ISqlConnectionFactory>(
                new SqlConnectionFactory(ConnectionString));

            services.RemoveAll<IDistributedCache>();

            services.AddDistributedMemoryCache();

            services.RemoveAll<ICacheService>();

            services.AddSingleton<ICacheService, CacheService>();

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, EmailService>();

            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    TestAuthenticationHandler.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    TestAuthenticationHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions,
                TestAuthenticationHandler>(
                TestAuthenticationHandler.AuthenticationScheme,
                _ => { });
        });
    }
}
