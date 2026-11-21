using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using System.Security.Cryptography;
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

    private const string SelectorScheme = "TestOrBearer";

    public const string CsrfHeaderName = "X-CSRF";

    public HttpClient Client { get; private set; } = default!;

    public FakeEmailService Emails { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    public IntegrationTestFactory()
    {
        _postgres = new PostgreSqlBuilder("postgres:18.6")
            .WithDatabase("ecommerce")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        // The auth cookies are Secure, and the clients' cookie jar only sends Secure cookies over https.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // No cookie jar: this client is shared by every test, so cookies from one test's login must not reach the next.
        Client = CreateClientWithoutCookieJar();

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

    // A client that sends exactly the auth cookies given here, the way a browser would send them.
    // Use it to present a specific token, for example one that was already rotated.
    public HttpClient CreateClientWithCookies(string? accessToken = null, string? refreshToken = null)
    {
        HttpClient client = CreateClientWithoutCookieJar();

        List<string> cookies = [];

        if (accessToken is not null)
        {
            cookies.Add($"{AuthCookieNames.AccessToken}={accessToken}");
        }

        if (refreshToken is not null)
        {
            cookies.Add($"{AuthCookieNames.RefreshToken}={refreshToken}");
        }

        if (cookies.Count > 0)
        {
            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        }

        return client;
    }

    public HttpClient CreateClientWithoutCookieJar()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = ClientOptions.BaseAddress,
            HandleCookies = false
        });
    }

    // Every test client sends the CSRF header a browser frontend sends. CsrfHeaderTests removes it.
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        client.DefaultRequestHeaders.Add(CsrfHeaderName, "1");
    }

    public override async ValueTask DisposeAsync()
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

            services.RemoveAll<NpgsqlDataSource>();

            services.AddSingleton(_ => NpgsqlDataSource.Create(ConnectionString));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

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
