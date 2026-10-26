using System.Net;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Coupons;

public sealed class DeleteCouponTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public DeleteCouponTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteCoupon_ShouldReturnConflict_WhenAnOrderHasUsedIt()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Coupon coupon = CouponData.Create();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(user, category, keyboard, coupon);
            await db.SaveChangesAsync();
        });

        using HttpClient customerClient = _factory.CreateClientForUser(user.Id.Value);
        using HttpClient adminClient = _factory.CreateAdminClient();

        await OrderData.PlaceAsync(customerClient, keyboard, 1, "SAVE10");

        // Act
        HttpResponseMessage response = await adminClient.DeleteAsync($"/api/v1/coupons/{coupon.Id.Value}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
