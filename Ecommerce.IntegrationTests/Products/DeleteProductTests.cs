using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Orders;
using Ecommerce.Application.Orders.GetAllOrders;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Products;

public sealed class DeleteProductTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public DeleteProductTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturnConflict_WhenTheProductHasBeenOrdered()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        await PlaceOrderAsync(client, keyboard, 1);

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/products/{keyboard.Id.Value}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient client, Product product, int quantity, string? couponCode = null)
    {
        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(product.Id.Value, quantity)], couponCode);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
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
