using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Categories.DeleteCategory;
internal sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        categoryRepository.Remove(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
