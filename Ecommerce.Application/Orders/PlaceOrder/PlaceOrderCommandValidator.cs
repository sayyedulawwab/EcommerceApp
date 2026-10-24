using Ecommerce.Domain.Coupons;
using FluentValidation;

namespace Ecommerce.Application.Orders.PlaceOrder;
public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.OrderStockItems).NotEmpty();
        RuleFor(c => c.CouponCode).MaximumLength(CouponCode.MaxLength);
        RuleForEach(c => c.OrderStockItems).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
