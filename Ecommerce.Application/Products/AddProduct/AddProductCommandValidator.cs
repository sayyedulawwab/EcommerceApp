using FluentValidation;

namespace Ecommerce.Application.Products.AddProduct;
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.description).NotEmpty();
        RuleFor(c => c.priceCurrency).NotEmpty();
        RuleFor(c => c.priceAmount).NotEmpty();
        RuleFor(c => c.quantity).NotEmpty();
        RuleFor(c => c.productCategoryId).NotEmpty();

    }
}
