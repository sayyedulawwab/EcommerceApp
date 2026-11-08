using System.Net;
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

    [Fact]
    public async Task SearchProduct_ShouldReturnEveryRowOnce_WhenPagingThroughRowsWithTheSameSortValue()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        List<Product> products = [.. Enumerable.Range(0, 5).Select(_ => ProductData.CreateProduct(category.Id, name: "Same name"))];

        await SeedAsync(category);
        await SeedAsync([.. products]);

        using HttpClient client = _factory.CreateClient();

        // Act
        List<Guid> seen = [];

        for (int page = 1; page <= 3; page++)
        {
            PagedResponse<ProductResponse> result = await SearchAsync(client, $"/api/v1/products?page={page}&pageSize=2&sortColumn=Name");

            seen.AddRange(result.Items.Select(p => p.Id));
        }

        // Assert
        Assert.Equal(5, seen.Count);
        Assert.Equal(products.Select(p => p.Id.Value).Order(), seen.Order());
    }

    [Theory]
    [InlineData("", 1, 2, 5, 2)]
    [InlineData("", 3, 2, 5, 1)]
    [InlineData("", 4, 2, 5, 0)]
    [InlineData("keyboard", 1, 2, 3, 2)]
    [InlineData("keyboard", 2, 2, 3, 1)]
    [InlineData("keyboard", 5, 2, 3, 0)]
    [InlineData("nothing matches this", 1, 2, 0, 0)]
    public async Task SearchProduct_ShouldReturnTheTotalOfAllMatches_OnEveryPage(string keyword, int page, int pageSize, int expectedTotal, int expectedItems)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await SeedAsync(category);
        await SeedAsync(
            ProductData.CreateProduct(category.Id, name: "Mechanical keyboard"),
            ProductData.CreateProduct(category.Id, name: "Wireless keyboard"),
            ProductData.CreateProduct(category.Id, name: "Keyboard tray"),
            ProductData.CreateProduct(category.Id, name: "Mouse"),
            ProductData.CreateProduct(category.Id, name: "Monitor"));

        using HttpClient client = _factory.CreateClient();

        // Act
        PagedResponse<ProductResponse> result = await SearchAsync(client, $"/api/v1/products?page={page}&pageSize={pageSize}&keyword={Uri.EscapeDataString(keyword)}");

        // Assert
        Assert.Equal(expectedTotal, result.TotalCount);
        Assert.Equal(expectedItems, result.Items.Count);
        Assert.Equal(page > 1, result.HasPreviousPage);
        Assert.Equal(page * pageSize < expectedTotal, result.HasNextPage);
    }

    [Theory]
    [InlineData("100%", "100% cotton shirt")]
    [InlineData("e_c", "snake_case lamp")]
    [InlineData("k\\s", "back\\slash")]
    public async Task SearchProduct_ShouldMatchTheKeywordLiterally_WhenItContainsALikeWildcard(string keyword, string expectedName)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await SeedAsync(category);
        await SeedAsync(
            ProductData.CreateProduct(category.Id, name: "100% cotton shirt"),
            ProductData.CreateProduct(category.Id, name: "100 cotton shirt"),
            ProductData.CreateProduct(category.Id, name: "snake_case lamp"),
            ProductData.CreateProduct(category.Id, name: "snakeXcase lamp"),
            ProductData.CreateProduct(category.Id, name: "back\\slash"),
            ProductData.CreateProduct(category.Id, name: "backslash"));

        using HttpClient client = _factory.CreateClient();

        // Act
        PagedResponse<ProductResponse> result = await SearchAsync(client, $"/api/v1/products?page=1&pageSize=10&keyword={Uri.EscapeDataString(keyword)}");

        // Assert
        Assert.Equal([expectedName], result.Items.Select(p => p.Name));
    }

    [Theory]
    [InlineData("pageSize=10&sortColumn=Quantity")]
    [InlineData("pageSize=101")]
    public async Task SearchProduct_ShouldReturnBadRequest_WhenTheSearchIsInvalid(string queryString)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/products?page=1&{queryString}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<PagedResponse<ProductResponse>> SearchAsync(HttpClient client)
    {
        return SearchAsync(client, SearchUrl);
    }

    private static async Task<PagedResponse<ProductResponse>> SearchAsync(HttpClient client, string url)
    {
        PagedResponse<ProductResponse>? page = await client.GetFromJsonAsync<PagedResponse<ProductResponse>>(url, cancellationToken: TestContext.Current.CancellationToken);

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
