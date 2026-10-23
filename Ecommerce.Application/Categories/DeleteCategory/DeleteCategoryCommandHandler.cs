using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Categories.DeleteCategory;
internal sealed class DeleteCategoryCommandHandler(
    IApplicationDbContext dbContext)
    : ICommandHandler<DeleteCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        dbContext.Categories.Remove(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
