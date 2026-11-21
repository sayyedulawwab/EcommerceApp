using Ecommerce.Domain.Categories;
using FluentValidation;

namespace Ecommerce.Application.Categories.AddCategory;
public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(CategoryName.MaxLength);
        RuleFor(c => c.Code).NotEmpty().MaximumLength(CategoryCode.MaxLength);
    }
}
