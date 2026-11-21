using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.API.Controllers.Categories;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Ecommerce.IntegrationTests.Auth;

// These tests send the cookie the login endpoint sets, so they run the real JWT validation and role checks.
public sealed class AccessTokenCookieTests
{
    private const string Password = "Password123!";

    private readonly IntegrationTestFactory _factory;

    public AccessTokenCookieTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCategory_ShouldReturnUnauthorized_WhenThereIsNoCookie()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/categories", new AddCategoryRequest("Electronics", "ELEC"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ShouldReturnUnauthorized_WhenTheTokenSignatureIsChanged()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string token = await LoginAsync(isAdmin: true);
        string[] parts = token.Split('.');
        string tamperedToken = $"{parts[0]}.{parts[1]}.{Reverse(parts[2])}";

        using HttpClient client = _factory.CreateClientWithCookies(accessToken: tamperedToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/categories", new AddCategoryRequest("Electronics", "ELEC"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ShouldReturnUnauthorized_WhenAValidTokenIsSentAsABearerHeader()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string token = await LoginAsync(isAdmin: true);

        using HttpClient client = _factory.CreateClientWithoutCookieJar();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/categories", new AddCategoryRequest("Electronics", "ELEC"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert: the token is accepted only from the cookie.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.Created)]
    public async Task AddCategory_ShouldOnlyAllowAnAdminToken(bool isAdmin, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = await CreateSignedInClientAsync(isAdmin);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/categories", new AddCategoryRequest("Electronics", "ELEC"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldIssueAnAccessTokenValidFor15MinutesWithTheUserIdClaim()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string token = await LoginAsync(isAdmin: false);

        UserId userId = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<User>().Select(user => user.Id).SingleAsync());

        // Act
        Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ReadJsonWebToken(token);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(15), jwt.ValidTo - jwt.ValidFrom);
        Assert.Equal("HS256", jwt.Alg);
        Assert.Equal(userId.Value.ToString(), jwt.Claims.Single(claim => claim.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
    }

    [Fact]
    public async Task Login_ShouldSetBothTokensAsHttpOnlySecureStrictCookies_AndReturnNoBody()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await AddUserAsync(isAdmin: false);

        // Act
        HttpResponseMessage response = await PostLoginAsync(_factory.Client);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        SetCookieHeaderValue? access = SetCookies.Find(response, AuthCookieNames.AccessToken);
        SetCookieHeaderValue? refresh = SetCookies.Find(response, AuthCookieNames.RefreshToken);

        Assert.NotNull(access);
        Assert.True(access.HttpOnly);
        Assert.True(access.Secure);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Strict, access.SameSite);
        Assert.Equal("/", access.Path.ToString());
        Assert.Null(access.Expires);

        Assert.NotNull(refresh);
        Assert.True(refresh.HttpOnly);
        Assert.True(refresh.Secure);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Strict, refresh.SameSite);
        Assert.Equal(AuthCookieNames.RefreshTokenPath, refresh.Path.ToString());
        Assert.InRange(refresh.Expires!.Value, DateTimeOffset.UtcNow.AddDays(7).AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(7).AddMinutes(1));
    }

    // The client's cookie jar stores the cookies login sets and sends them on, like a browser.
    private async Task<HttpClient> CreateSignedInClientAsync(bool isAdmin)
    {
        await AddUserAsync(isAdmin);

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await PostLoginAsync(client);
        response.EnsureSuccessStatusCode();

        return client;
    }

    private async Task<string> LoginAsync(bool isAdmin)
    {
        await AddUserAsync(isAdmin);

        HttpResponseMessage response = await PostLoginAsync(_factory.Client);

        return SetCookies.GetValue(response, AuthCookieNames.AccessToken);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client)
    {
        return client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("jane@example.com", Password), cancellationToken: TestContext.Current.CancellationToken);
    }

    private Task AddUserAsync(bool isAdmin)
    {
        return _factory.ExecuteScopeAsync(async sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();

            var user = User.Create(
                FirstName.Create("Jane").Value,
                LastName.Create("Doe").Value,
                Email.Create("jane@example.com").Value,
                authService.HashPassword(Password),
                isAdmin);

            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(user);
            await db.SaveChangesAsync();
        });
    }

    private static string Reverse(string value)
    {
        char[] chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
