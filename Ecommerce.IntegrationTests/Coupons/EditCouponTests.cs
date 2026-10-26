using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Coupons;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Coupons;

public sealed class EditCouponTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public EditCouponTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EditCoupon_ShouldReturnConflict_WhenTheLimitIsBelowTheUsesSoFar()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);
        Coupon coupon = CouponData.Create(maxRedemptions: 5);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(user, category, keyboard, coupon);
            await db.SaveChangesAsync();
        });

        using HttpClient customerClient = _factory.CreateClientForUser(user.Id.Value);
        using HttpClient adminClient = _factory.CreateAdminClient();

        await OrderData.PlaceAsync(customerClient, keyboard, 1, "SAVE10");
        await OrderData.PlaceAsync(customerClient, keyboard, 1, "SAVE10");

        var request = new EditCouponRequest(10, coupon.StartsOnUtc, coupon.ExpiresOnUtc, 1);

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/coupons/{coupon.Id.Value}", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
