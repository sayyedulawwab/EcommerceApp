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

        Result<CategoryName> name = CategoryName.Create(request.Name);
        Result<CategoryCode> code = CategoryCode.Create(request.Code);

        if (name.IsFailure || code.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.FromResults([name, code]));
        }

        Error duplicate = await FindDuplicateAsync(category.Id, name.Value, code.Value, cancellationToken);

        if (duplicate != Error.None)
        {
            return Result.Failure<Guid>(duplicate);
        }

        category = Category.Update(
            category,
            name.Value,
            code.Value,
            dateTimeProvider.UtcNow);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request took the name or code between the check and the save: a unique index rejects this one.
            duplicate = await FindDuplicateAsync(category.Id, name.Value, code.Value, cancellationToken);

            if (duplicate != Error.None)
            {
                return Result.Failure<Guid>(duplicate);
            }

            throw;
        }

        return category.Id.Value;
    }

    // Another category with the same name or code. The category being edited may keep its own.
    private async Task<Error> FindDuplicateAsync(CategoryId id, CategoryName name, CategoryCode code, CancellationToken cancellationToken)
    {
        if (await dbContext.Categories.AnyAsync(category => category.Id != id && category.Name == name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists;
        }

        if (await dbContext.Categories.AnyAsync(category => category.Id != id && category.Code == code, cancellationToken))
        {
            return CategoryErrors.CodeAlreadyExists;
        }

        return Error.None;
    }
}
