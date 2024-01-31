using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.ProductCategories.GetAllProductCategories;
internal sealed class GetAllProductCategoriesQueryHandler : IQueryHandler<GetAllProductCategoriesQuery, IReadOnlyList<ProductCategoryResponse>>
{
    private readonly IProductCategoryRepository _productCategoryRepository;
    public GetAllProductCategoriesQueryHandler(IProductCategoryRepository productCategoryRepository)
    {
        _productCategoryRepository = productCategoryRepository;
    }
    public async Task<Result<IReadOnlyList<ProductCategoryResponse>>> Handle(GetAllProductCategoriesQuery request, CancellationToken cancellationToken)
    {

        var productCategories = await _productCategoryRepository.GetAllAsync();

        var productCategoriesResponse = productCategories.Select(cat => new ProductCategoryResponse()
        {
            Id = cat.Id.Value,
            Name = cat.Name,
            Code = cat.Code
        });

        return productCategoriesResponse.ToList();
    }
}
