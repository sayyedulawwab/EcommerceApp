using System.Net.Http.Json;
using Ecommerce.Application.Products;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Products;

// The search result is cached in the Redis container, so these tests run against the same cache as production.
public sealed class SearchProductTests
{
    private const string SearchUrl = "/api/v1/products?page=1&pageSize=10";

    private readonly IntegrationTestFactory _factory;

    public SearchProductTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchProduct_ShouldServeTheCachedResult_UntilAProductIsChangedThroughTheApi()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await SeedAsync(category, ProductData.CreateProduct(category.Id, name: "Keyboard"));

        using HttpClient anonymousClient = _factory.CreateClient();

        await SearchAsync(anonymousClient);

        // This row skips the handlers, so nothing removes the cached search.
        await SeedAsync(ProductData.CreateProduct(category.Id, name: "Mouse"));

        // Act
        PagedResponse<ProductResponse> cached = await SearchAsync(anonymousClient);

        using HttpClient adminClient = _factory.CreateAdminClient();

        await adminClient.PostAsJsonAsync("/api/v1/products", ProductData.Create(category.Id.Value), cancellationToken: TestContext.Current.CancellationToken);

        PagedResponse<ProductResponse> afterAdd = await SearchAsync(anonymousClient);

        // Assert
        Assert.Equal(["Keyboard"], cached.Items.Select(p => p.Name));
        Assert.Equal(3, afterAdd.Items.Count);
    }

    private static async Task<PagedResponse<ProductResponse>> SearchAsync(HttpClient client)
    {
        PagedResponse<ProductResponse>? page = await client.GetFromJsonAsync<PagedResponse<ProductResponse>>(SearchUrl, cancellationToken: TestContext.Current.CancellationToken);

        return page!;
    }

    private Task SeedAsync(params object[] entities)
    {
        return _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(entities);
            await db.SaveChangesAsync();
        });
    }
}
