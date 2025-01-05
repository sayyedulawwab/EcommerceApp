using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.EditCategory;
internal sealed class EditCategoryCommandHandler(
    ICategoryRepository categoryRepository, 
    IUnitOfWork unitOfWork, 
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        category = Category.Update(
            category, 
            new CategoryName(request.Name), 
            new CategoryCode(request.Code),
            dateTimeProvider.UtcNow);

        categoryRepository.Update(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
