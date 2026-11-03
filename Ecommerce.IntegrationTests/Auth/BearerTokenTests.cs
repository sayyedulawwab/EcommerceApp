using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.API.Controllers.Categories;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Users.Login;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Auth;

// These tests send the token the login endpoint issues, so they run the real JWT validation and role checks.
public sealed class BearerTokenTests
{
    private const string Password = "Password123!";

    private readonly IntegrationTestFactory _factory;

    public BearerTokenTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOk_WhenTheTokenComesFromLogin()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = await CreateSignedInClientAsync(isAdmin: false);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/orders", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ShouldReturnUnauthorized_WhenThereIsNoToken()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/orders", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ShouldReturnUnauthorized_WhenTheTokenSignatureIsChanged()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        string token = await LoginAsync(isAdmin: false);
        string[] parts = token.Split('.');
        string tamperedToken = $"{parts[0]}.{parts[1]}.{Reverse(parts[2])}";

        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/orders", TestContext.Current.CancellationToken);

        // Assert
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

    private async Task<HttpClient> CreateSignedInClientAsync(bool isAdmin)
    {
        string token = await LoginAsync(isAdmin);

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private async Task<string> LoginAsync(bool isAdmin)
    {
        await _factory.ExecuteScopeAsync(async sp =>
        {
            IAuthService authService = sp.GetRequiredService<IAuthService>();
            string salt = authService.GenerateSalt();

            var user = User.Create(
                FirstName.Create("Jane").Value,
                LastName.Create("Doe").Value,
                Email.Create("jane@example.com").Value,
                authService.HashPassword(Password, salt),
                salt,
                isAdmin);

            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(user);
            await db.SaveChangesAsync();
        });

        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginUserRequest("jane@example.com", Password), cancellationToken: TestContext.Current.CancellationToken);

        AccessTokenResponse? token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken: TestContext.Current.CancellationToken);

        return token!.AccessToken;
    }

    private static string Reverse(string value)
    {
        char[] chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
