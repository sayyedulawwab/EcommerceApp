using FluentValidation;

namespace Ecommerce.Application.Products.DeleteProduct;
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(c => c.id).NotEmpty();
    }
}
