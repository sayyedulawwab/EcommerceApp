using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

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

        var category = Category.Create(name.Value, code.Value, dateTimeProvider.UtcNow);

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
