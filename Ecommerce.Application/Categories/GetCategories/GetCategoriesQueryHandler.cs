using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.GetCategories;
internal sealed class GetCategoriesQueryHandler(ICategoryRepository categoryRepository) 
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {

        IReadOnlyList<Category> productCategories = await categoryRepository.GetAllAsync(cancellationToken);

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
