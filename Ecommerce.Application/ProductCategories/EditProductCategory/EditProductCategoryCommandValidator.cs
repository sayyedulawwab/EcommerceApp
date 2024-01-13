using FluentValidation;

namespace Ecommerce.Application.ProductCategories.EditProductCategory;
public class EditProductCategoryCommandValidator : AbstractValidator<EditProductCategoryCommand>
{
    public EditProductCategoryCommandValidator()
    {
        RuleFor(c => c.id).NotEmpty();
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.code).NotEmpty();

    }
}
