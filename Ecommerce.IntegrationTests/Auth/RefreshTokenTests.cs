using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Users;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task Login_ShouldReturnARefreshToken_AndStoreOnlyItsHash()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();

        // Act
        AuthTokensResponse tokens = await LoginAsync();

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
    public async Task Refresh_ShouldReturnNewTokensAndRevokeTheOldOne_WhenTheTokenIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        AuthTokensResponse first = await LoginAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync(first.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AuthTokensResponse? second = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(second);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        List<RefreshToken> stored = await GetStoredTokensAsync();

        Assert.Equal(2, stored.Count);
        Assert.True(stored.Single(token => token.TokenHash == HashOf(first.RefreshToken)).IsRevoked);
        Assert.False(stored.Single(token => token.TokenHash == HashOf(second.RefreshToken)).IsRevoked);

        // The new access token works.
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.AccessToken);

        HttpResponseMessage orders = await client.GetAsync("/api/v1/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, orders.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldRevokeEveryTokenOfTheUser_WhenARotatedTokenIsUsedAgain()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        AuthTokensResponse first = await LoginAsync();
        AuthTokensResponse otherSession = await LoginAsync();

        HttpResponseMessage rotation = await RefreshAsync(first.RefreshToken);
        AuthTokensResponse? second = await rotation.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Act: the first token was already used once.
        HttpResponseMessage reuse = await RefreshAsync(first.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.All(await GetStoredTokensAsync(), token => Assert.True(token.IsRevoked));

        // Neither the newer token nor the other session can refresh any more.
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second!.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(otherSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldNotRevokeTheTokensOfAnotherUser_WhenARotatedTokenIsUsedAgain()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        await RegisterAsync("john@example.com");
        AuthTokensResponse jane = await LoginAsync();
        AuthTokensResponse john = await LoginAsync("john@example.com");

        await RefreshAsync(jane.RefreshToken);

        // Act
        await RefreshAsync(jane.RefreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(john.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldAcceptOnlyOneRequest_WhenTheSameTokenIsUsedAtTheSameTime()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        AuthTokensResponse tokens = await LoginAsync();

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RefreshAsync(tokens.RefreshToken)));

        // Assert
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTheTokenIsUnknown()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync("this-token-was-never-issued");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.InvalidRefreshToken.Name, problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorizedWithoutRevokingOtherTokens_WhenTheTokenIsExpired()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        AuthTokensResponse active = await LoginAsync();

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
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(active.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturnBadRequest_WhenTheTokenIsEmpty()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync("");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeThePresentedToken_WhenTheTokenIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        AuthTokensResponse tokens = await LoginAsync();

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/logout", new LogoutRequest(tokens.RefreshToken), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldKeepOtherSessionsSignedIn_WhenOneTokenIsRevoked()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await RegisterAsync();
        AuthTokensResponse tokens = await LoginAsync();
        AuthTokensResponse otherSession = await LoginAsync();

        // Act
        await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/logout", new LogoutRequest(tokens.RefreshToken), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(otherSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldReturnNoContent_WhenTheTokenIsUnknown()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/logout", new LogoutRequest("this-token-was-never-issued"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldReturnBadRequest_WhenTheTokenIsEmpty()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/logout", new LogoutRequest(""), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task RegisterAsync(string email = Email)
    {
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterUserRequest("Jane", "Doe", email, Password), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<AuthTokensResponse> LoginAsync(string email = Email)
    {
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginUserRequest(email, Password), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken: TestContext.Current.CancellationToken))!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        return _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequest(refreshToken), cancellationToken: TestContext.Current.CancellationToken);
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
}
