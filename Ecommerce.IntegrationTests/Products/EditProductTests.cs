using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Products;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Products;

public sealed class EditProductTests
{
    private readonly IntegrationTestFactory _factory;

    public EditProductTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EditProduct_ShouldUpdateTheProduct_WhenRequestIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(category, keyboard);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/products/{keyboard.Id.Value}", CreateRequest(category.Id.Value), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Product saved = await FindAsync(keyboard.Id);

        Assert.Equal("Wireless Keyboard", saved.Name.Value);
        Assert.Equal(120m, saved.Price.Amount);
        Assert.Equal(5, saved.Quantity);
        Assert.NotNull(saved.UpdatedOnUtc);
    }

    [Fact]
    public async Task EditProduct_ShouldReturnBadRequest_WhenThePriceIsNotPositive()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(category, keyboard);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/products/{keyboard.Id.Value}", CreateRequest(category.Id.Value) with { PriceAmount = 0 }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Validation.General", problem.Extensions["code"]?.ToString());

        Product saved = await FindAsync(keyboard.Id);

        Assert.Equal(100m, saved.Price.Amount);
    }

    [Fact]
    public async Task EditProduct_ShouldReturnNotFound_WhenTheProductDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await SeedAsync(category);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/products/{Guid.NewGuid()}", CreateRequest(category.Id.Value), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static EditProductRequest CreateRequest(Guid categoryId)
    {
        return new EditProductRequest
        {
            Name = "Wireless Keyboard",
            Description = "Low-profile wireless keyboard",
            PriceCurrency = "USD",
            PriceAmount = 120m,
            Quantity = 5,
            CategoryId = categoryId
        };
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

    private Task<Product> FindAsync(ProductId id)
    {
        return _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Product>().AsNoTracking().SingleAsync(p => p.Id == id));
    }
}
