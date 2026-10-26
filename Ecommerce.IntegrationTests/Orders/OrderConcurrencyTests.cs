using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Orders;
using Ecommerce.Application.Exceptions;
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

public sealed class OrderConcurrencyTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public OrderConcurrencyTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowConcurrencyException_WhenTwoOrdersRedeemTheLastCouponUse()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Coupon coupon = CouponData.Create(maxRedemptions: 1);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(coupon);
            await db.SaveChangesAsync();
        });

        // Two requests read the coupon before either saves, so both see one use left.
        using IServiceScope first = _factory.CreateScope();
        using IServiceScope second = _factory.CreateScope();
        ApplicationDbContext firstDb = first.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ApplicationDbContext secondDb = second.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Coupon firstCopy = await firstDb.Set<Coupon>().FirstAsync(c => c.Id == coupon.Id, cancellationToken: TestContext.Current.CancellationToken);
        Coupon secondCopy = await secondDb.Set<Coupon>().FirstAsync(c => c.Id == coupon.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(firstCopy.Redeem(DateTime.UtcNow).IsSuccess);
        Assert.True(secondCopy.Redeem(DateTime.UtcNow).IsSuccess);

        await firstDb.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act and assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => secondDb.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowConcurrencyException_WhenTheOrderWasCancelledAfterItWasLoaded()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(user, category, keyboard);
            await db.SaveChangesAsync();
        });

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        HttpResponseMessage placed = await client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest([new PlaceOrderProductRequest(keyboard.Id.Value, 3)]), cancellationToken: TestContext.Current.CancellationToken);
        var orderId = new OrderId(await placed.Content.ReadFromJsonAsync<Guid>(cancellationToken: TestContext.Current.CancellationToken));

        // A second cancel request reads the order while it is still placed.
        using IServiceScope scope = _factory.CreateScope();
        ApplicationDbContext staleDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Order staleOrder = await staleDb.Set<Order>().Include(o => o.OrderItems).FirstAsync(o => o.Id == orderId, cancellationToken: TestContext.Current.CancellationToken);
        Dictionary<ProductId, Product> staleProducts = await staleDb.Set<Product>().Where(p => p.Id == keyboard.Id).ToDictionaryAsync(p => p.Id, cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage cancelled = await client.PostAsync($"/api/v1/orders/{orderId.Value}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);

        Assert.True(OrderCancellationService.Cancel(staleOrder, staleProducts, coupon: null, DateTime.UtcNow).IsSuccess);

        // Act and assert: the stale cancel can't give the stock back a second time.
        await Assert.ThrowsAsync<ConcurrencyException>(() => staleDb.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
