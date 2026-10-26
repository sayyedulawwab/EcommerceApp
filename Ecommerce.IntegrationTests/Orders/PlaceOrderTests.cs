using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Orders;
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
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

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

        Product?[] stock = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return new[]
            {
                await db.Set<Product>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == keyboard.Id),
                await db.Set<Product>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == mouse.Id)
            };
        });

        Assert.Equal(8, stock[0]!.Quantity);
        Assert.Equal(7, stock[1]!.Quantity);
    }

    [Fact]
    public async Task PlaceOrder_ShouldSendConfirmationEmail_WhenOrderIsPlaced()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        // Seeding the user raised its own welcome email.
        _factory.Emails.Clear();

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 2)]);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        SentEmail email = Assert.Single(_factory.Emails.Sent);
        Assert.Equal(user.Email.Value, email.To);
        Assert.Equal("Your order is placed", email.Subject);
        Assert.Contains("200.00 USD", email.Body, StringComparison.Ordinal);
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
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

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
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

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
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenQuantityIsNotPositive()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 0)]);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnConflictAndKeepStock_WhenStockIsInsufficient()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, keyboard.Quantity + 1)]);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        (int orders, int quantity) = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            int orderCount = await db.Set<Order>().CountAsync(o => o.UserId == user.Id);
            Product stored = await db.Set<Product>().AsNoTracking().FirstAsync(p => p.Id == keyboard.Id);

            return (orderCount, stored.Quantity);
        });

        Assert.Equal(0, orders);
        Assert.Equal(10, quantity);
        Assert.DoesNotContain(_factory.Emails.Sent, email => email.Subject == "Your order is placed");
    }

    [Fact]
    public async Task PlaceOrder_ShouldApplyTheDiscountAndCountTheRedemption_WhenTheCouponIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Coupon coupon = CouponData.Create(percentOff: 10);

        await SeedAsync(user, category, keyboard, coupon);

        // Lower case on purpose: codes are matched in upper case.
        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 2)], "save10");

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        (Order order, Coupon storedCoupon) = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return (
                await db.Set<Order>().AsNoTracking().FirstAsync(o => o.UserId == user.Id),
                await db.Set<Coupon>().AsNoTracking().FirstAsync(c => c.Id == coupon.Id));
        });

        Assert.Equal(20m, order.Discount.Amount);
        Assert.Equal(180m, order.TotalPrice.Amount);
        Assert.Equal(coupon.Id, order.CouponId);
        Assert.Equal(1, storedCoupon.RedemptionCount);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequestAndKeepStock_WhenTheCouponHasExpired()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Coupon coupon = CouponData.Create(expiresOnUtc: DateTime.UtcNow.AddDays(-1));

        await SeedAsync(user, category, keyboard, coupon);

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 2)], "SAVE10");

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        int quantity = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return (await db.Set<Product>().AsNoTracking().FirstAsync(p => p.Id == keyboard.Id)).Quantity;
        });

        Assert.Equal(10, quantity);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnConflict_WhenTheCouponIsUsedUp()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Coupon coupon = CouponData.Create(maxRedemptions: 1);

        await SeedAsync(user, category, keyboard, coupon);

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 1)], "SAVE10");

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnNotFound_WhenTheCouponDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        var request = new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 1)], "NOPE10");

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
