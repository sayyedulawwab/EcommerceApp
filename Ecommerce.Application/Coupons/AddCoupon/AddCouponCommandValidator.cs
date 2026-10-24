using Ecommerce.Domain.Coupons;
using FluentValidation;

namespace Ecommerce.Application.Coupons.AddCoupon;
public class AddCouponCommandValidator : AbstractValidator<AddCouponCommand>
{
    public AddCouponCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(CouponCode.MaxLength);
        RuleFor(c => c.PercentOff).InclusiveBetween(1, 100);
        RuleFor(c => c.ExpiresOnUtc).GreaterThan(c => c.StartsOnUtc);
        RuleFor(c => c.MaxRedemptions).GreaterThan(0);
    }
}
