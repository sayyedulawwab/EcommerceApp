using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Coupons;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Domain.UnitTests.Coupons;

public class CouponTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly CouponCode Code = CouponCode.Create("SAVE10").Value;

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_Should_ReturnInvalidPercentOff_WhenPercentIsOutOfRange(int percentOff)
    {
        Result<Coupon> result = Coupon.Create(Code, percentOff, UtcNow, UtcNow.AddDays(1), 1, UtcNow);

        Assert.Equal(CouponErrors.InvalidPercentOff, result.Error);
    }

    [Fact]
    public void Create_Should_ReturnInvalidPeriod_WhenItExpiresBeforeItStarts()
    {
        Result<Coupon> result = Coupon.Create(Code, 10, UtcNow, UtcNow, 1, UtcNow);

        Assert.Equal(CouponErrors.InvalidPeriod, result.Error);
    }

    [Fact]
    public void Redeem_Should_ReturnNotActive_BeforeItStarts()
    {
        Coupon coupon = Coupon.Create(Code, 10, UtcNow.AddDays(1), UtcNow.AddDays(2), 1, UtcNow).Value;

        Assert.Equal(CouponErrors.NotActive, coupon.Redeem(UtcNow).Error);
    }

    [Fact]
    public void Redeem_Should_ReturnExpired_AtTheExpiryTime()
    {
        Coupon coupon = Coupon.Create(Code, 10, UtcNow.AddDays(-2), UtcNow, 1, UtcNow).Value;

        Assert.Equal(CouponErrors.Expired, coupon.Redeem(UtcNow).Error);
    }

    [Fact]
    public void Redeem_Should_ReturnUsageLimitReached_WhenEveryUseIsTaken()
    {
        Coupon coupon = Coupon.Create(Code, 10, UtcNow, UtcNow.AddDays(1), 1, UtcNow).Value;
        coupon.Redeem(UtcNow);

        Result result = coupon.Redeem(UtcNow);

        Assert.Equal(CouponErrors.UsageLimitReached, result.Error);
        Assert.Equal(1, coupon.RedemptionCount);
    }

    [Fact]
    public void CalculateDiscount_Should_RoundToCents()
    {
        Coupon coupon = Coupon.Create(Code, 15, UtcNow, UtcNow.AddDays(1), 1, UtcNow).Value;

        Money discount = coupon.CalculateDiscount(new Money(33.33m, Currency.Usd));

        Assert.Equal(new Money(5.00m, Currency.Usd), discount);
    }

    [Theory]
    [InlineData("save10", "SAVE10")]
    [InlineData(" Spring-Sale ", "SPRING-SALE")]
    public void CouponCode_Should_NormalizeToUpperCase(string input, string expected)
    {
        Assert.Equal(expected, CouponCode.Create(input).Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("AB")]
    [InlineData("SAVE 10")]
    public void CouponCode_Should_ReturnInvalid_WhenTheCodeIsNotAllowed(string? input)
    {
        Assert.Equal(CouponCode.Invalid, CouponCode.Create(input).Error);
    }
}
