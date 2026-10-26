using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Orders;
using Ecommerce.Application.Orders.SearchOrders;
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

public sealed class CancelOrderTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public CancelOrderTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CancelOrder_ShouldGiveBackStockAndTheCouponRedemption_WhenTheOrderIsPlaced()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Coupon coupon = CouponData.Create(maxRedemptions: 1);

        await SeedAsync(user, category, keyboard, coupon);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        Guid orderId = await PlaceOrderAsync(client, keyboard, 3, "SAVE10");

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        (Order order, int quantity, int redemptions) = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return (
                await db.Set<Order>().AsNoTracking().FirstAsync(o => o.Id == new OrderId(orderId)),
                (await db.Set<Product>().AsNoTracking().FirstAsync(p => p.Id == keyboard.Id)).Quantity,
                (await db.Set<Coupon>().AsNoTracking().FirstAsync(c => c.Id == coupon.Id)).RedemptionCount);
        });

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancelledOnUtc);
        Assert.Equal(10, quantity);
        Assert.Equal(0, redemptions);
    }

    [Fact]
    public async Task CancelOrder_ShouldSucceedWithoutGivingStockBackTwice_WhenTheOrderIsAlreadyCancelled()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        Guid orderId = await PlaceOrderAsync(client, keyboard, 3);
        await client.PostAsync($"/api/v1/orders/{orderId}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        int quantity = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return (await db.Set<Product>().AsNoTracking().FirstAsync(p => p.Id == keyboard.Id)).Quantity;
        });

        Assert.Equal(10, quantity);
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnNotFound_WhenTheOrderBelongsToAnotherUser()
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

        Guid orderId = await PlaceOrderAsync(ownerClient, keyboard, 1);

        // Act
        HttpResponseMessage response = await otherClient.PostAsync($"/api/v1/orders/{orderId}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
