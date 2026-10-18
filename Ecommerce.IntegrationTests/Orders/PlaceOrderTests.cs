using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Orders;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Orders;

public sealed class PlaceOrderTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public PlaceOrderTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlaceOrder_ShouldCreateOrderWithTotalPrice_WhenRequestIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Product mouse = ProductData.CreateProduct(category.Id, 25m, name: "Mouse");

        await SeedAsync(user, category, keyboard, mouse);

        var request = new PlaceOrderRequest(
        [
            new PlaceOrderProductRequest(keyboard.Id.Value, 2),
            new PlaceOrderProductRequest(mouse.Id.Value, 3)
        ]);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Order? order = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return await db.Set<Order>()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.UserId == user.Id);
        });

        Assert.NotNull(order);
        Assert.Equal(275m, order.TotalPrice.Amount);
        Assert.Equal("USD", order.TotalPrice.Currency.Code);
        Assert.Equal(2, order.OrderItems.Count);
        Assert.Null(order.UpdatedOnUtc);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();

        await SeedAsync(user);

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(Guid.NewGuid(), 1)]);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenProductsHaveDifferentCurrencies()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product usdProduct = ProductData.CreateProduct(category.Id, 10m, "USD");
        Product bdtProduct = ProductData.CreateProduct(category.Id, 10m, "BDT", "Cable");

        await SeedAsync(user, category, usdProduct, bdtProduct);

        var request = new PlaceOrderRequest(
        [
            new PlaceOrderProductRequest(usdProduct.Id.Value, 1),
            new PlaceOrderProductRequest(bdtProduct.Id.Value, 1)
        ]);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenOrderHasNoItems()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new PlaceOrderRequest([]);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
