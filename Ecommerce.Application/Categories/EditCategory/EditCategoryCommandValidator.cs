using Ecommerce.Domain.Categories;
using FluentValidation;

namespace Ecommerce.Application.Categories.EditCategory;
public class EditCategoryCommandValidator : AbstractValidator<EditCategoryCommand>
{
    public EditCategoryCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(CategoryName.MaxLength);
        RuleFor(c => c.Code).NotEmpty().MaximumLength(CategoryCode.MaxLength);
    }
}
