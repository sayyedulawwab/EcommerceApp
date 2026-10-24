using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Domain.Coupons;

// A percentage discount that is valid for a period and for a limited number of orders.
public sealed class Coupon : Entity<CouponId>
{
    private Coupon(CouponId id, CouponCode code, int percentOff, DateTime startsOnUtc, DateTime expiresOnUtc, int maxRedemptions, DateTime createdOnUtc) : base(id)
    {
        Code = code;
        PercentOff = percentOff;
        StartsOnUtc = startsOnUtc;
        ExpiresOnUtc = expiresOnUtc;
        MaxRedemptions = maxRedemptions;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by EF Core to materialize the entity
    private Coupon()
    {
    }
    #pragma warning restore CS8618

    public CouponCode Code { get; private set; }
    public int PercentOff { get; private set; }
    public DateTime StartsOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public int MaxRedemptions { get; private set; }
    public int RedemptionCount { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Result<Coupon> Create(CouponCode code, int percentOff, DateTime startsOnUtc, DateTime expiresOnUtc, int maxRedemptions, DateTime createdOnUtc)
    {
        Result validation = Validate(percentOff, startsOnUtc, expiresOnUtc, maxRedemptions);

        if (validation.IsFailure)
        {
            return Result.Failure<Coupon>(validation.Error);
        }

        return new Coupon(CouponId.New(), code, percentOff, startsOnUtc, expiresOnUtc, maxRedemptions, createdOnUtc);
    }

    // The code can't change: customers may already have it, and orders reference the coupon.
    public Result Update(int percentOff, DateTime startsOnUtc, DateTime expiresOnUtc, int maxRedemptions, DateTime updatedOnUtc)
    {
        Result validation = Validate(percentOff, startsOnUtc, expiresOnUtc, maxRedemptions);

        if (validation.IsFailure)
        {
            return validation;
        }

        if (maxRedemptions < RedemptionCount)
        {
            return Result.Failure(CouponErrors.MaxRedemptionsBelowUsage);
        }

        PercentOff = percentOff;
        StartsOnUtc = startsOnUtc;
        ExpiresOnUtc = expiresOnUtc;
        MaxRedemptions = maxRedemptions;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Success();
    }

    private static Result Validate(int percentOff, DateTime startsOnUtc, DateTime expiresOnUtc, int maxRedemptions)
    {
        if (percentOff is < 1 or > 100)
        {
            return Result.Failure(CouponErrors.InvalidPercentOff);
        }

        if (expiresOnUtc <= startsOnUtc)
        {
            return Result.Failure(CouponErrors.InvalidPeriod);
        }

        if (maxRedemptions < 1)
        {
            return Result.Failure(CouponErrors.InvalidMaxRedemptions);
        }

        return Result.Success();
    }

    public Result Redeem(DateTime utcNow)
    {
        if (utcNow < StartsOnUtc)
        {
            return Result.Failure(CouponErrors.NotActive);
        }

        if (utcNow >= ExpiresOnUtc)
        {
            return Result.Failure(CouponErrors.Expired);
        }

        if (RedemptionCount >= MaxRedemptions)
        {
            return Result.Failure(CouponErrors.UsageLimitReached);
        }

        RedemptionCount++;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }

    // Gives the redemption back when the order that used it is cancelled.
    public void ReleaseRedemption(DateTime utcNow)
    {
        if (RedemptionCount == 0)
        {
            throw new InvalidOperationException("The coupon has no redemption to release.");
        }

        RedemptionCount--;
        UpdatedOnUtc = utcNow;
    }

    // Rounded to cents, half away from zero, so 10% of 99.95 is 10.00, not 9.99.
    public Money CalculateDiscount(Money subtotal)
    {
        decimal amount = Math.Round(subtotal.Amount * PercentOff / 100m, 2, MidpointRounding.AwayFromZero);

        return new Money(amount, subtotal.Currency);
    }
}
