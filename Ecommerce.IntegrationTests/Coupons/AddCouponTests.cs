using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Coupons;
using Ecommerce.Domain.Coupons;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Coupons;

public sealed class AddCouponTests
    : IClassFixture<IntegrationTestFactory>
{
    private static readonly AddCouponRequest Request = new("summer-20", 20, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), 100);

    private readonly IntegrationTestFactory _factory;

    public AddCouponTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCoupon_ShouldCreateTheCouponWithAnUpperCaseCode_WhenTheUserIsAnAdmin()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/coupons", Request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Coupon coupon = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Coupon>().AsNoTracking().SingleAsync());

        Assert.Equal("SUMMER-20", coupon.Code.Value);
        Assert.Equal(20, coupon.PercentOff);
    }

    [Fact]
    public async Task AddCoupon_ShouldReturnConflict_WhenTheCodeAlreadyExists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient adminClient = _factory.CreateAdminClient();

        await adminClient.PostAsJsonAsync("/api/v1/coupons", Request, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/coupons", Request with { Code = "SUMMER-20" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCoupon_ShouldReturnForbidden_WhenTheUserIsNotAnAdmin()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient customerClient = _factory.CreateClientForUser(Guid.NewGuid());

        // Act
        HttpResponseMessage response = await customerClient.PostAsJsonAsync("/api/v1/coupons", Request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
