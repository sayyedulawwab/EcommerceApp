using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.API.Controllers.Categories;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class CsrfHeaderTests
{
    private readonly IntegrationTestFactory _factory;

    public CsrfHeaderTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCategory_ShouldReturnForbidden_WhenTheCsrfHeaderIsMissing()
    {
        // Arrange: a signed-in admin, as a forged request from another site would arrive with the cookies.
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateAdminClient();
        client.DefaultRequestHeaders.Remove(IntegrationTestFactory.CsrfHeaderName);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/categories", new AddCategoryRequest("Electronics", "ELEC"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Request.MissingCsrfHeader", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Login_ShouldReturnForbidden_WhenTheCsrfHeaderIsMissing()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClientWithoutCookieJar();
        client.DefaultRequestHeaders.Remove(IntegrationTestFactory.CsrfHeaderName);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginUserRequest("nobody@example.com", "Password123!"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ShouldNotNeedTheCsrfHeader()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClientWithoutCookieJar();
        client.DefaultRequestHeaders.Remove(IntegrationTestFactory.CsrfHeaderName);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/v1/categories",TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
