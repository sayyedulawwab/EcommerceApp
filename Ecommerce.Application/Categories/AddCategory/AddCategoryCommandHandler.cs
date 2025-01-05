using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.AddCategory;
internal sealed class AddCategoryCommandHandler(
    ICategoryRepository categoryRepository, 
    IUnitOfWork unitOfWork, 
    IDateTimeProvider dateTimeProvider) 
    : ICommandHandler<AddCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(new CategoryName(request.Name), new CategoryCode(request.Code), dateTimeProvider.UtcNow);

        categoryRepository.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
