using FluentValidation;

namespace Ecommerce.Application.Categories.AddCategory;
public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
    }
}
