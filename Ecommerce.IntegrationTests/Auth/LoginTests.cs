using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.API.Controllers.Auth;
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
            new FirstName("Legacy"),
            new LastName("User"),
            new Email("legacy@example.com"),
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
}
