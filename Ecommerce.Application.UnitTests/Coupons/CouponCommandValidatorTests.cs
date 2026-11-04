using Ecommerce.Application.Coupons.AddCoupon;
using Ecommerce.Application.Coupons.EditCoupon;
using Ecommerce.Domain.Coupons;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Coupons;

public class CouponCommandValidatorTests
{
    private static readonly DateTime StartsOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly AddCouponCommandValidator AddValidator = new();
    private static readonly EditCouponCommandValidator EditValidator = new();
    private static readonly AddCouponCommand ValidAddCommand = new("SAVE10", 10, StartsOnUtc, StartsOnUtc.AddDays(30), 100);
    private static readonly EditCouponCommand ValidEditCommand = new(Guid.CreateVersion7(), 10, StartsOnUtc, StartsOnUtc.AddDays(30), 100);

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandsAreValid()
    {
        Assert.True(AddValidator.Validate(ValidAddCommand).IsValid);
        Assert.True(EditValidator.Validate(ValidEditCommand).IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCodeIsEmpty()
    {
        ValidationResult result = AddValidator.Validate(ValidAddCommand with { Code = "" });

        ValidationAssert.HasSingleError(result, nameof(AddCouponCommand.Code));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCodeIsLongerThanMaxLength()
    {
        ValidationResult result = AddValidator.Validate(ValidAddCommand with { Code = new string('A', CouponCode.MaxLength + 1) });

        ValidationAssert.HasSingleError(result, nameof(AddCouponCommand.Code));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_Should_ReturnError_WhenPercentOffIsOutsideOneToHundred(int percentOff)
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { PercentOff = percentOff }), nameof(AddCouponCommand.PercentOff));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { PercentOff = percentOff }), nameof(EditCouponCommand.PercentOff));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_Should_ReturnNoErrors_WhenPercentOffIsOneOrHundred(int percentOff)
    {
        Assert.True(AddValidator.Validate(ValidAddCommand with { PercentOff = percentOff }).IsValid);
        Assert.True(EditValidator.Validate(ValidEditCommand with { PercentOff = percentOff }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_ReturnError_WhenExpiryIsNotAfterStart(int daysAfterStart)
    {
        DateTime expiresOnUtc = StartsOnUtc.AddDays(daysAfterStart);

        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { ExpiresOnUtc = expiresOnUtc }), nameof(AddCouponCommand.ExpiresOnUtc));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { ExpiresOnUtc = expiresOnUtc }), nameof(EditCouponCommand.ExpiresOnUtc));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenMaxRedemptionsIsZero()
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { MaxRedemptions = 0 }), nameof(AddCouponCommand.MaxRedemptions));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { MaxRedemptions = 0 }), nameof(EditCouponCommand.MaxRedemptions));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenEditIdIsEmpty()
    {
        ValidationResult result = EditValidator.Validate(ValidEditCommand with { Id = Guid.Empty });

        ValidationAssert.HasSingleError(result, nameof(EditCouponCommand.Id));
    }
}
