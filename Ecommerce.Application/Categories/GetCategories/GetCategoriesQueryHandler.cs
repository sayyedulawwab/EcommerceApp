using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Categories.GetCategories;
internal sealed class GetCategoriesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {

        IReadOnlyList<Category> productCategories = await dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken);

        IEnumerable<CategoryResponse> productCategoriesResponse = productCategories
            .Select(cat => new CategoryResponse()
            {
                Id = cat.Id.Value,
                Name = cat.Name.Value,
                Code = cat.Code.Value
            });

        return productCategoriesResponse.ToList();
    }
}
