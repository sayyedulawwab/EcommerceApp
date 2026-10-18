using Ecommerce.Domain.Shared;
using FluentValidation;

namespace Ecommerce.Application.Products.EditProduct;
public class EditProductCommandValidator : AbstractValidator<EditProductCommand>
{
    public EditProductCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.PriceCurrency).Must(Currency.IsSupported).WithMessage("The currency code is not supported.");
        RuleFor(c => c.PriceAmount).GreaterThan(0);
        RuleFor(c => c.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CategoryId).NotEmpty();

    }
}
