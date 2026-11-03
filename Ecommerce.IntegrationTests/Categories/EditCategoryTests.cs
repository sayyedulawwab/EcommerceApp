using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Categories;
using Ecommerce.Domain.Categories;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Categories;

public sealed class EditCategoryTests
{
    private readonly IntegrationTestFactory _factory;

    public EditCategoryTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EditCategory_ShouldUpdateTheCategory_WhenRequestIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create("Electronics", "ELEC");

        await SeedAsync(category);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/categories/{category.Id.Value}", new EditCategoryRequest("Computers", "COMP"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Category saved = await FindAsync(category.Id);

        Assert.Equal("Computers", saved.Name.Value);
        Assert.Equal("COMP", saved.Code.Value);
        Assert.NotNull(saved.UpdatedOnUtc);
    }

    [Fact]
    public async Task EditCategory_ShouldKeepItsOwnName_WhenOnlyTheCodeChanges()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create("Electronics", "ELEC");

        await SeedAsync(category);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/categories/{category.Id.Value}", new EditCategoryRequest("Electronics", "ELEC-2"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ELEC-2", (await FindAsync(category.Id)).Code.Value);
    }

    [Fact]
    public async Task EditCategory_ShouldReturnConflict_WhenAnotherCategoryHasTheName()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category electronics = CategoryData.Create("Electronics", "ELEC");
        Category computers = CategoryData.Create("Computers", "COMP");

        await SeedAsync(electronics, computers);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/categories/{computers.Id.Value}", new EditCategoryRequest("Electronics", "COMP"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Computers", (await FindAsync(computers.Id)).Name.Value);
    }

    [Fact]
    public async Task EditCategory_ShouldReturnNotFound_WhenTheCategoryDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/categories/{Guid.NewGuid()}", new EditCategoryRequest("Computers", "COMP"), cancellationToken: TestContext.Current.CancellationToken);

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

    private Task<Category> FindAsync(CategoryId id)
    {
        return _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Category>().AsNoTracking().SingleAsync(c => c.Id == id));
    }
}
