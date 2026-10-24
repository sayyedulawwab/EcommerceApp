using Ecommerce.Domain.Coupons;

namespace Ecommerce.IntegrationTests.Helpers;

internal static class CouponData
{
    public static Coupon Create(
        string code = "SAVE10",
        int percentOff = 10,
        int maxRedemptions = 100,
        DateTime? expiresOnUtc = null)
    {
        return Coupon.Create(
            CouponCode.Create(code).Value,
            percentOff,
            DateTime.UtcNow.AddDays(-30),
            expiresOnUtc ?? DateTime.UtcNow.AddDays(30),
            maxRedemptions,
            DateTime.UtcNow.AddDays(-30)).Value;
    }
}
