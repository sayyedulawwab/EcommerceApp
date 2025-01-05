using FluentValidation;

namespace Ecommerce.Application.Categories.EditCategory;
public class EditCategoryCommandValidator : AbstractValidator<EditCategoryCommand>
{
    public EditCategoryCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();

    }
}
