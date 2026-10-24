using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Users.Login;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class LoginTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public LoginTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnAccessToken_WhenUserRegisteredWithSamePassword()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var registerRequest = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!");

        HttpResponseMessage registerResponse = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("jane@example.com", "Password123!"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AccessTokenResponse? token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!"));

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("jane@example.com", "WrongPassword!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("missing@example.com", "Password123!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnAccessToken_WhenUserHasLegacySha256PasswordHash()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        string legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("Password123!" + salt)));

        var user = User.Create(
            FirstName.Create("Legacy").Value,
            LastName.Create("User").Value,
            Email.Create("legacy@example.com").Value,
            legacyHash,
            salt,
            isAdmin: false);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            db.Add(user);

            await db.SaveChangesAsync();
        });

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("legacy@example.com", "Password123!"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_ShouldPutTheAdminRoleInTheToken_OnlyForAnAdmin(bool isAdmin)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();
            string salt = authService.GenerateSalt();

            var user = User.Create(
                FirstName.Create("Ada").Value,
                LastName.Create("Admin").Value,
                Email.Create("ada@example.com").Value,
                authService.HashPassword("Password123!", salt),
                salt,
                isAdmin);

            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(user);
            await db.SaveChangesAsync();
        });

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("ada@example.com", "Password123!"));

        // Assert
        AccessTokenResponse? token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        // The JWT payload is the second, base64url-encoded part of the token. Claims keep their full ClaimTypes URI.
        string payload = token!.AccessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var json = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload));

        bool hasAdminRole = json.RootElement.TryGetProperty(System.Security.Claims.ClaimTypes.Role, out System.Text.Json.JsonElement role) && role.GetString() == Roles.Admin;

        Assert.Equal(isAdmin, hasAdminRole);
    }
}
