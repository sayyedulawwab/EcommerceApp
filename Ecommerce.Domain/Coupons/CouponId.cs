namespace Ecommerce.Domain.Coupons;

public record CouponId(Guid Value)
{
    public static CouponId New() => new(Guid.CreateVersion7());
}
