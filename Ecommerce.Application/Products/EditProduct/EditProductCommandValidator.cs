using FluentValidation;

namespace Ecommerce.Application.Products.EditProduct;
public class EditProductCommandValidator : AbstractValidator<EditProductCommand>
{
    public EditProductCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Description).NotEmpty();
        RuleFor(c => c.PriceCurrency).NotEmpty();
        RuleFor(c => c.PriceAmount).NotEmpty();
        RuleFor(c => c.Quantity).NotEmpty();
        RuleFor(c => c.CategoryId).NotEmpty();

    }
}
