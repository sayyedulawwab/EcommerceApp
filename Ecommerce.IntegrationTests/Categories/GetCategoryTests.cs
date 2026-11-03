using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.Categories;
using Ecommerce.Domain.Categories;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Categories;

public sealed class GetCategoryTests
{
    private readonly IntegrationTestFactory _factory;

    public GetCategoryTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategories_ShouldReturnEveryCategory_WithoutSigningIn()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await SeedAsync(CategoryData.Create("Electronics", "ELEC"), CategoryData.Create("Computers", "COMP"));

        using HttpClient anonymousClient = _factory.CreateClient();

        // Act
        List<CategoryResponse>? categories = await anonymousClient.GetFromJsonAsync<List<CategoryResponse>>("/api/v1/categories", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(categories);
        Assert.Equal(["Computers", "Electronics"], categories.Select(c => c.Name).Order());
    }

    [Fact]
    public async Task GetCategory_ShouldReturnTheCategory_WhenItExists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create("Electronics", "ELEC");

        await SeedAsync(category);

        using HttpClient anonymousClient = _factory.CreateClient();

        // Act
        CategoryResponse? response = await anonymousClient.GetFromJsonAsync<CategoryResponse>($"/api/v1/categories/{category.Id.Value}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(category.Id.Value, response.Id);
        Assert.Equal("Electronics", response.Name);
        Assert.Equal("ELEC", response.Code);
    }

    [Fact]
    public async Task GetCategory_ShouldReturnNotFound_WhenItDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient anonymousClient = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await anonymousClient.GetAsync($"/api/v1/categories/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task SeedAsync(params Category[] categories)
    {
        return _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(categories);
            await db.SaveChangesAsync();
        });
    }
}
