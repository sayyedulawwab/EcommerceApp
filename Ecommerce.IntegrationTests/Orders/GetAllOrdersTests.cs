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

namespace Ecommerce.IntegrationTests.Orders;

public sealed class GetAllOrdersTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public GetAllOrdersTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOnlyTheUsersOwnOrdersWithThePricePaid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User owner = UserData.Create();
        User otherUser = UserData.Create(email: "other@example.com");
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(owner, otherUser, category, keyboard);

        using HttpClient ownerClient = _factory.CreateClientForUser(owner.Id.Value);
        using HttpClient otherClient = _factory.CreateClientForUser(otherUser.Id.Value);

        await PlaceOrderAsync(ownerClient, keyboard, 1);
        await PlaceOrderAsync(otherClient, keyboard, 1);

        // The price changes after the orders were placed.
        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            Product product = await db.Set<Product>().FirstAsync(p => p.Id == keyboard.Id);
            Product.Update(product, product.Name, product.Description, new Domain.Shared.Money(150m, product.Price.Currency), product.Quantity, product.CategoryId, DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        // Act
        List<OrderResponse>? orders = await ownerClient.GetFromJsonAsync<List<OrderResponse>>("/api/v1/orders");

        // Assert
        OrderResponse order = Assert.Single(orders!);
        Assert.Equal(owner.Id.Value, order.UserId);
        Assert.Equal(100m, Assert.Single(order.OrderItems).PriceAmount);
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
