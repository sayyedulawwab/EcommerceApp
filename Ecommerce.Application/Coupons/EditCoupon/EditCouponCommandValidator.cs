using FluentValidation;

namespace Ecommerce.Application.Coupons.EditCoupon;
public class EditCouponCommandValidator : AbstractValidator<EditCouponCommand>
{
    public EditCouponCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.PercentOff).InclusiveBetween(1, 100);
        RuleFor(c => c.ExpiresOnUtc).GreaterThan(c => c.StartsOnUtc);
        RuleFor(c => c.MaxRedemptions).GreaterThan(0);
    }
}
