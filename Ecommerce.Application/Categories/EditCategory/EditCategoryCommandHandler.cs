using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Categories.EditCategory;
internal sealed class EditCategoryCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        category = Category.Update(
            category,
            new CategoryName(request.Name),
            new CategoryCode(request.Code),
            dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
