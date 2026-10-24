using FluentValidation;

namespace Ecommerce.Application.Coupons.DeleteCoupon;
public class DeleteCouponCommandValidator : AbstractValidator<DeleteCouponCommand>
{
    public DeleteCouponCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
