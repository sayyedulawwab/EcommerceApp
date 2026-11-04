using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using FluentValidation;

namespace Ecommerce.Application.Products.AddProduct;
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(ProductName.MaxLength);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(ProductDescription.MaxLength);
        RuleFor(c => c.PriceCurrency).Must(Currency.IsSupported).WithMessage("The currency code is not supported.");
        RuleFor(c => c.PriceAmount).GreaterThan(0);
        RuleFor(c => c.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CategoryId).NotEmpty();

    }
}
