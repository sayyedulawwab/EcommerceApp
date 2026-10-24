namespace Ecommerce.Application.Coupons;
public sealed class CouponResponse
{
    public Guid Id { get; init; }
    public required string Code { get; init; }
    public int PercentOff { get; init; }
    public DateTime StartsOnUtc { get; init; }
    public DateTime ExpiresOnUtc { get; init; }
    public int MaxRedemptions { get; init; }
    public int RedemptionCount { get; init; }
}
