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
        var category = Category.Create(new CategoryName(request.Name), new CategoryCode(request.Code), dateTimeProvider.UtcNow);

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;

    }
}
