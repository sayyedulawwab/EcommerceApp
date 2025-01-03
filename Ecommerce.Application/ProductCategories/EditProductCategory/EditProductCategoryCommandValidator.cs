using FluentValidation;

namespace Ecommerce.Application.ProductCategories.EditProductCategory;
public class EditProductCategoryCommandValidator : AbstractValidator<EditProductCategoryCommand>
{
    public EditProductCategoryCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();

    }
}
