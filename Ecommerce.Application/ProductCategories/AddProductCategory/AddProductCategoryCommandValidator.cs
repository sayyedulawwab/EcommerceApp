using FluentValidation;

namespace Ecommerce.Application.ProductCategories.AddProductCategory;
public class AddProductCategoryCommandValidator : AbstractValidator<AddProductCategoryCommand>
{
    public AddProductCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
    }
}
