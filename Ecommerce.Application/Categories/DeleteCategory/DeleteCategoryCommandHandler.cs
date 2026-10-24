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

        if (await HasProductsAsync(category.Id, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.HasProducts);
        }

        dbContext.Categories.Remove(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A product added between the check and the delete hits the foreign key. Report it as the same conflict.
            if (await HasProductsAsync(category.Id, cancellationToken))
            {
                return Result.Failure<Guid>(CategoryErrors.HasProducts);
            }

            throw;
        }

        return category.Id.Value;
    }

    private Task<bool> HasProductsAsync(CategoryId categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Products.AnyAsync(product => product.CategoryId == categoryId, cancellationToken);
    }
}
