using FluentValidation;

namespace Ecommerce.Application.ProductCategories.DeleteProductCategory;

public class DeleteProductCategoryCommandValidator : AbstractValidator<DeleteProductCategoryCommand>
{
    public DeleteProductCategoryCommandValidator()
    {
        RuleFor(c => c.id).NotEmpty();
    }
}
