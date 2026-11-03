using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Categories.AddCategory;
internal sealed class AddCategoryCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        Result<CategoryName> name = CategoryName.Create(request.Name);
        Result<CategoryCode> code = CategoryCode.Create(request.Code);

        if (name.IsFailure || code.IsFailure)
        {
            return Result.Failure<Guid>(ValidationError.FromResults([name, code]));
        }

        Error duplicate = await FindDuplicateAsync(name.Value, code.Value, cancellationToken);

        if (duplicate != Error.None)
        {
            return Result.Failure<Guid>(duplicate);
        }

        var category = Category.Create(name.Value, code.Value, dateTimeProvider.UtcNow);

        dbContext.Categories.Add(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two admins adding the same name or code at once: a unique index rejects the second one.
            duplicate = await FindDuplicateAsync(name.Value, code.Value, cancellationToken);

            if (duplicate != Error.None)
            {
                return Result.Failure<Guid>(duplicate);
            }

            throw;
        }

        return category.Id.Value;
    }

    private async Task<Error> FindDuplicateAsync(CategoryName name, CategoryCode code, CancellationToken cancellationToken)
    {
        if (await dbContext.Categories.AnyAsync(category => category.Name == name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists;
        }

        if (await dbContext.Categories.AnyAsync(category => category.Code == code, cancellationToken))
        {
            return CategoryErrors.CodeAlreadyExists;
        }

        return Error.None;
    }
}
