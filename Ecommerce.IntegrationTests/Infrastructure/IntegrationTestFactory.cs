using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using System.Security.Cryptography;
using StackExchange.Redis;
using System.Data.Common;
using System.Net.Http.Headers;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ecommerce.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    private readonly RedisContainer _redis;

    private ConnectionMultiplexer _redisConnection = default!;

    private DatabaseReset _databaseReset = default!;

    private DbConnection _connection = default!;

    private const string SelectorScheme = "TestOrBearer";

    public HttpClient Client { get; private set; } = default!;

    public FakeEmailService Emails { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    public IntegrationTestFactory()
    {
        _postgres = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("ecommerce")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redis = new RedisBuilder("redis:8")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // allowAdmin lets the reset flush the cache between tests.
        _redisConnection = await ConnectionMultiplexer.ConnectAsync($"{_redis.GetConnectionString()},allowAdmin=true");

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

    public async Task ResetDatabaseAsync()
    {
        Emails.Clear();

        await _databaseReset.ResetAsync(_connection);

        // A cached query result from an earlier test would hide the rows this test seeds.
        foreach (IServer server in _redisConnection.GetServers())
        {
            await server.FlushAllDatabasesAsync();
        }

        // Flushing Redis does not clear the in-memory copies the app keeps in front of it.
        // "*" is HybridCache's tag for every entry.
        HybridCache cache = Services.GetRequiredService<HybridCache>();

        await cache.RemoveByTagAsync("*");
    }

    // A signed-in admin: the only role that can change products, categories and coupons.
    public HttpClient CreateAdminClient()
    {
        HttpClient client = CreateClientForUser(TestAuthenticationHandler.UserId);

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeaderName, Roles.Admin);

        return client;
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

    public override async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        if (_redisConnection is not null)
        {
            await _redisConnection.DisposeAsync();
        }

        await _postgres.DisposeAsync();

        await _redis.DisposeAsync();

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
                    ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                    // A new random signing key for every run: no key is committed anywhere.
                    ["Jwt:SecretKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                    // The lowest accepted bcrypt cost, so the many logins in these tests stay fast.
                    ["PasswordHashing:WorkFactor"] = "10",
                    // High enough that the many auth calls in these tests are never throttled. RateLimitTests lowers it.
                    ["RateLimiting:Credentials:PermitLimit"] = "100000",
                    ["RateLimiting:Tokens:PermitLimit"] = "100000"
                }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(ConnectionString)
                       .UseSnakeCaseNamingConvention());

            services.RemoveAll<ISqlConnectionFactory>();

            services.AddSingleton<ISqlConnectionFactory>(
                new SqlConnectionFactory(ConnectionString));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

            // Cache only in Redis, which ResetDatabaseAsync flushes. With the in-memory layer on, a search
            // cached by one test was still served to the next one on the CI runners, despite the reset.
            services.PostConfigure<HybridCacheOptions>(options =>
            {
                HybridCacheEntryOptions? defaults = options.DefaultEntryOptions;

                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = defaults?.Expiration,
                    LocalCacheExpiration = defaults?.LocalCacheExpiration,
                    Flags = (defaults?.Flags ?? HybridCacheEntryFlags.None) | HybridCacheEntryFlags.DisableLocalCache
                };
            });

            // A request with "Authorization: Test" signs in through TestAuthenticationHandler.
            // Any other request goes through the app's real JWT bearer handler, so a test can
            // send a real token, a bad token or no token at all.
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    _ => { })
                .AddPolicyScheme(SelectorScheme, SelectorScheme, options =>
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.Authorization.ToString() == TestAuthenticationHandler.AuthenticationScheme
                            ? TestAuthenticationHandler.AuthenticationScheme
                            : JwtBearerDefaults.AuthenticationScheme);

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = SelectorScheme;
                options.DefaultAuthenticateScheme = SelectorScheme;
                options.DefaultChallengeScheme = SelectorScheme;
            });
        });
    }
}
