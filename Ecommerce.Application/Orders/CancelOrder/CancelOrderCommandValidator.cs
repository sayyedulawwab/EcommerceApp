using FluentValidation;

namespace Ecommerce.Application.Orders.CancelOrder;
public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
