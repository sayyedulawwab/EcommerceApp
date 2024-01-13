using FluentValidation;

namespace Ecommerce.Application.ProductCategories.AddProductCategory;
public class AddProductCategoryCommandValidator : AbstractValidator<AddProductCategoryCommand>
{
    public AddProductCategoryCommandValidator()
    {
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.code).NotEmpty();
    }
}
