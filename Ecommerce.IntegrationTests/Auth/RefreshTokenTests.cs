using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class RefreshTokenTests
{
    private const string Email = "jane@example.com";
    private const string Password = "Password123!";

    private readonly IntegrationTestFactory _factory;

    public RefreshTokenTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldSetARefreshTokenCookie_AndStoreOnlyItsHash()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();

        // Act
        Tokens tokens = await LoginAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));

        List<RefreshToken> stored = await GetStoredTokensAsync();

        RefreshToken single = Assert.Single(stored);
        Assert.NotEqual(tokens.RefreshToken, single.TokenHash);
        Assert.Equal(64, single.TokenHash.Length);
        Assert.Equal(HashOf(tokens.RefreshToken), single.TokenHash);
        Assert.False(single.IsRevoked);
    }

    [Fact]
    public async Task Refresh_ShouldSetNewCookiesAndRevokeTheOldToken_WhenTheTokenIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        Tokens first = await LoginAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync(first.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Tokens second = ReadTokens(response);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        List<RefreshToken> stored = await GetStoredTokensAsync();

        Assert.Equal(2, stored.Count);
        Assert.True(stored.Single(token => token.TokenHash == HashOf(first.RefreshToken)).IsRevoked);
        Assert.False(stored.Single(token => token.TokenHash == HashOf(second.RefreshToken)).IsRevoked);

        // The new access token works.
        using HttpClient client = _factory.CreateClientWithCookies(accessToken: second.AccessToken);

        HttpResponseMessage orders = await client.GetAsync("/api/v1/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, orders.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldKeepTheUserSignedIn_WhenTheBrowserSendsTheCookiesItWasGiven()
    {
        // Arrange: a client with a cookie jar behaves like a browser.
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();

        using HttpClient browser = _factory.CreateClient();

        (await browser.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginUserRequest(Email, Password), cancellationToken: TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage refresh = await browser.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);
        HttpResponseMessage orders = await browser.GetAsync("/api/v1/orders", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.OK, orders.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldRevokeEveryTokenOfTheUser_WhenARotatedTokenIsUsedAgain()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        Tokens first = await LoginAsync();
        Tokens otherSession = await LoginAsync();

        HttpResponseMessage rotation = await RefreshAsync(first.RefreshToken);
        Tokens second = ReadTokens(rotation);

        // Act: the first token was already used once.
        HttpResponseMessage reuse = await RefreshAsync(first.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.All(await GetStoredTokensAsync(), token => Assert.True(token.IsRevoked));

        // Neither the newer token nor the other session can refresh any more.
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(otherSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldNotRevokeTheTokensOfAnotherUser_WhenARotatedTokenIsUsedAgain()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        await RegisterAsync("john@example.com");
        Tokens jane = await LoginAsync();
        Tokens john = await LoginAsync("john@example.com");

        await RefreshAsync(jane.RefreshToken);

        // Act
        await RefreshAsync(jane.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, (await RefreshAsync(john.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldAcceptOnlyOneRequest_WhenTheSameTokenIsUsedAtTheSameTime()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        Tokens tokens = await LoginAsync();

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RefreshAsync(tokens.RefreshToken)));

        // Assert
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorizedAndClearTheCookies_WhenTheTokenIsUnknown()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync("this-token-was-never-issued");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.InvalidRefreshToken.Name, problem!.Extensions["code"]?.ToString());
        AssertCookiesCleared(response);
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorizedWithoutRevokingOtherTokens_WhenTheTokenIsExpired()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        Tokens active = await LoginAsync();

        string expiredToken = "expired-token-value";

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            IJwtService jwtService = sp.GetRequiredService<IJwtService>();
            UserId userId = await db.Set<User>().Select(user => user.Id).SingleAsync();

            db.Add(RefreshToken.Create(
                userId,
                jwtService.HashRefreshToken(expiredToken),
                DateTime.UtcNow.AddDays(-8),
                DateTime.UtcNow.AddDays(-1)));

            await db.SaveChangesAsync();
        });

        // Act
        HttpResponseMessage response = await RefreshAsync(expiredToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await RefreshAsync(active.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenThereIsNoCookie()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClientWithoutCookieJar();

        // Act
        HttpResponseMessage response = await client.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeTheTokenAndClearTheCookies_WhenTheTokenIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        Tokens tokens = await LoginAsync();

        // Act
        HttpResponseMessage response = await LogoutAsync(tokens.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AssertCookiesCleared(response);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldKeepOtherSessionsSignedIn_WhenOneTokenIsRevoked()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        Tokens tokens = await LoginAsync();
        Tokens otherSession = await LoginAsync();

        // Act
        await LogoutAsync(tokens.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, (await RefreshAsync(otherSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldReturnNoContent_WhenTheTokenIsUnknown()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await LogoutAsync("this-token-was-never-issued");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldReturnNoContentAndClearTheCookies_WhenThereIsNoCookie()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClientWithoutCookieJar();

        // Act
        HttpResponseMessage response = await client.PostAsync("/api/v1/auth/logout", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AssertCookiesCleared(response);
    }

    private async Task RegisterAsync(string email = Email)
    {
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterUserRequest("Jane", "Doe", email, Password), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<Tokens> LoginAsync(string email = Email)
    {
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginUserRequest(email, Password), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return ReadTokens(response);
    }

    private async Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        using HttpClient client = _factory.CreateClientWithCookies(refreshToken: refreshToken);

        return await client.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> LogoutAsync(string refreshToken)
    {
        using HttpClient client = _factory.CreateClientWithCookies(refreshToken: refreshToken);

        return await client.PostAsync("/api/v1/auth/logout", null, TestContext.Current.CancellationToken);
    }

    private static Tokens ReadTokens(HttpResponseMessage response)
    {
        return new Tokens(
            SetCookies.GetValue(response, AuthCookieNames.AccessToken),
            SetCookies.GetValue(response, AuthCookieNames.RefreshToken));
    }

    // A deleted cookie comes back empty with an expiry in the past.
    private static void AssertCookiesCleared(HttpResponseMessage response)
    {
        foreach (string name in new[] { AuthCookieNames.AccessToken, AuthCookieNames.RefreshToken })
        {
            SetCookieHeaderValue? cookie = SetCookies.Find(response, name);

            Assert.NotNull(cookie);
            Assert.Equal(string.Empty, cookie.Value.ToString());
            Assert.True(cookie.Expires < DateTimeOffset.UtcNow);
        }
    }

    private Task<List<RefreshToken>> GetStoredTokensAsync()
    {
        return _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<RefreshToken>().AsNoTracking().ToListAsync());
    }

    private string HashOf(string refreshToken)
    {
        using IServiceScope scope = _factory.CreateScope();

        return scope.ServiceProvider.GetRequiredService<IJwtService>().HashRefreshToken(refreshToken);
    }

    private sealed record Tokens(string AccessToken, string RefreshToken);
}
