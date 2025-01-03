using FluentValidation;

namespace Ecommerce.Application.Products.AddProduct;
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Description).NotEmpty();
        RuleFor(c => c.PriceCurrency).NotEmpty();
        RuleFor(c => c.PriceAmount).NotEmpty();
        RuleFor(c => c.Quantity).NotEmpty();
        RuleFor(c => c.ProductCategoryId).NotEmpty();

    }
}
