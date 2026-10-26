using System.Net;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Categories;

public sealed class DeleteCategoryTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public DeleteCategoryTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnConflictAndKeepTheProducts_WhenTheCategoryHasProducts()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(category, keyboard);
            await db.SaveChangesAsync();
        });

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.DeleteAsync($"/api/v1/categories/{category.Id.Value}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        bool productExists = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Product>().AnyAsync(p => p.Id == keyboard.Id));

        Assert.True(productExists);
    }
}
