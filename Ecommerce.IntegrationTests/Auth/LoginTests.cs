using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Users;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class LoginTests
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

        HttpResponseMessage registerResponse = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("jane@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AuthTokensResponse? token = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("jane@example.com", "WrongPassword!"), cancellationToken: TestContext.Current.CancellationToken);

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
            new LoginUserRequest("missing@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldRehashPassword_WhenStoredHashUsesAnotherWorkFactor()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Work factor 4 is the bcrypt minimum; the test host is configured with 10.
        string oldHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 4);

        await AddUserAsync("old@example.com", oldHash);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("old@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string storedHash = await GetPasswordHashAsync("old@example.com");

        Assert.NotEqual(oldHash, storedHash);
        Assert.StartsWith("$2a$10$", storedHash, StringComparison.Ordinal);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", storedHash));
    }

    [Fact]
    public async Task Login_ShouldKeepTheStoredHash_WhenItAlreadyUsesTheConfiguredWorkFactor()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string hash = BCrypt.Net.BCrypt.HashPassword("Password123!", 10);

        await AddUserAsync("current@example.com", hash);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("current@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(hash, await GetPasswordHashAsync("current@example.com"));
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenTheStoredHashIsNotBcrypt()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await AddUserAsync("legacy@example.com", "bGVnYWN5LWhhc2gtZnJvbS1iZWZvcmUtYmNyeXB0");

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("legacy@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenThePasswordOnlyMatchesTheFirst72Bytes()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string password = new('a', 72);

        await AddUserAsync("long@example.com", BCrypt.Net.BCrypt.HashPassword(password, 10));

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("long@example.com", password + "b"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

            var user = User.Create(
                FirstName.Create("Ada").Value,
                LastName.Create("Admin").Value,
                Email.Create("ada@example.com").Value,
                authService.HashPassword("Password123!"),
                isAdmin);

            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(user);
            await db.SaveChangesAsync();
        });

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("ada@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AuthTokensResponse? token = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // The JWT payload is the second, base64url-encoded part of the token. Claims keep their full ClaimTypes URI.
        string payload = token!.AccessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var json = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload));

        bool hasAdminRole = json.RootElement.TryGetProperty(System.Security.Claims.ClaimTypes.Role, out System.Text.Json.JsonElement role) && role.GetString() == Roles.Admin;

        Assert.Equal(isAdmin, hasAdminRole);
    }

    private Task AddUserAsync(string email, string passwordHash)
    {
        return _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            db.Add(User.Create(
                FirstName.Create("Test").Value,
                LastName.Create("User").Value,
                Email.Create(email).Value,
                passwordHash,
                isAdmin: false));

            await db.SaveChangesAsync();
        });
    }

    private Task<string> GetPasswordHashAsync(string email)
    {
        Email userEmail = Email.Create(email).Value;

        return _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>()
                .Set<User>()
                .AsNoTracking()
                .Where(user => user.Email == userEmail)
                .Select(user => user.PasswordHash)
                .SingleAsync());
    }
}
