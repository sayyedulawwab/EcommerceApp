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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Coupons;

public sealed class EditCouponTests
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

    [Fact]
    public async Task EditCoupon_ShouldReturnBadRequest_WhenItExpiresBeforeItStarts()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Coupon coupon = CouponData.Create();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(coupon);
            await db.SaveChangesAsync();
        });

        var request = new EditCouponRequest(10, coupon.ExpiresOnUtc, coupon.StartsOnUtc, 100);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/v1/coupons/{coupon.Id.Value}", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Validation.General", problem.Extensions["code"]?.ToString());
    }
}
