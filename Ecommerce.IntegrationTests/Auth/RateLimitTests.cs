using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class RateLimitTests
{
    private const int PermitLimit = 3;

    private readonly IntegrationTestFactory _factory;

    public RateLimitTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnTooManyRequests_WhenTheLimitIsExceeded()
    {
        // Arrange: a separate host with a small limit, so the shared host used by the other tests is never throttled.
        await _factory.ResetDatabaseAsync();

        using WebApplicationFactory<Program> factory = CreateFactoryWithLimit();
        using HttpClient client = factory.CreateClient();

        // Act
        List<HttpResponseMessage> responses = [];

        for (int i = 0; i < PermitLimit + 1; i++)
        {
            responses.Add(await client.PostAsJsonAsync(
                "/api/v1/auth/login", new LoginUserRequest("nobody@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken));
        }

        // Assert
        Assert.All(responses.Take(PermitLimit), response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));

        HttpResponseMessage rejected = responses[PermitLimit];

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Auth_ShouldShareOneLimitAcrossItsEndpoints_WhenTheSameClientCallsThem()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using WebApplicationFactory<Program> factory = CreateFactoryWithLimit();
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < PermitLimit; i++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/auth/logout", new LogoutRequest("some-token"), cancellationToken: TestContext.Current.CancellationToken);
        }

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequest("some-token"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldIgnoreXForwardedFor_WhenNoProxyIsTrusted()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using WebApplicationFactory<Program> factory = CreateFactoryWithLimit();
        using HttpClient client = factory.CreateClient();

        // Act: every request claims to come from a different address.
        List<HttpResponseMessage> responses = [];

        for (int i = 0; i < PermitLimit + 1; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new LoginUserRequest("nobody@example.com", "Password123!"))
            };

            request.Headers.Add("X-Forwarded-For", $"203.0.113.{i + 1}");

            responses.Add(await client.SendAsync(request, TestContext.Current.CancellationToken));
        }

        // Assert: a client cannot get a fresh bucket by changing the header.
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[PermitLimit].StatusCode);
    }

    [Fact]
    public async Task Login_ShouldCountEachForwardedClientSeparately_WhenTheRequestComesFromATrustedProxy()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using WebApplicationFactory<Program> factory = CreateFactoryBehindProxy();
        using HttpClient client = factory.CreateClient();

        // Act: four different clients, one more request than the limit allows for a single client.
        List<HttpResponseMessage> responses = [];

        for (int i = 0; i < PermitLimit + 1; i++)
        {
            responses.Add(await SendLoginAsync(client, $"203.0.113.{i + 1}"));
        }

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public async Task Login_ShouldUseTheAddressTheProxyAdded_WhenTheClientSendsItsOwnXForwardedFor()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using WebApplicationFactory<Program> factory = CreateFactoryBehindProxy();
        using HttpClient client = factory.CreateClient();

        // Act: nginx appends the real client address (198.51.100.7) after whatever the client sent.
        List<HttpResponseMessage> responses = [];

        for (int i = 0; i < PermitLimit + 1; i++)
        {
            responses.Add(await SendLoginAsync(client, $"203.0.113.{i + 1}, 198.51.100.7"));
        }

        // Assert: the made-up first address changes nothing, so the fourth request is still limited.
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[PermitLimit].StatusCode);
    }

    [Fact]
    public async Task OtherEndpoints_ShouldNotBeRateLimited_WhenTheAuthLimitIsExceeded()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using WebApplicationFactory<Program> factory = CreateFactoryWithLimit();
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < PermitLimit + 1; i++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/auth/login", new LoginUserRequest("nobody@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);
        }

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/products", TestContext.Current.CancellationToken);

        // Assert: the call is anonymous, so authorization answers 401, not the rate limiter.
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendLoginAsync(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginUserRequest("nobody@example.com", "Password123!"))
        };

        request.Headers.Add("X-Forwarded-For", forwardedFor);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // The test server has no network peer, so this host gives every request the address 10.0.0.5 and trusts that address as the proxy.
    private WebApplicationFactory<Program> CreateFactoryBehindProxy()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:PermitLimit"] = PermitLimit.ToString(CultureInfo.InvariantCulture),
                    ["RateLimiting:Auth:WindowSeconds"] = "600",
                    ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5"
                }));

            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, FakePeerAddressStartupFilter>());
        });
    }

    private sealed class FakePeerAddressStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

                    return nextMiddleware();
                });

                next(app);
            };
        }
    }

    private WebApplicationFactory<Program> CreateFactoryWithLimit()
    {
        return _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:PermitLimit"] = PermitLimit.ToString(CultureInfo.InvariantCulture),
                    ["RateLimiting:Auth:WindowSeconds"] = "600"
                })));
    }
}
