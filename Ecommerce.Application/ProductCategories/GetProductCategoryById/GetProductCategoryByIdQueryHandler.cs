using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.ProductCategories.GetProductCategoryById;
internal sealed class GetProductCategoryByIdQueryHandler : IQueryHandler<GetProductCategoryByIdQuery, ProductCategoryResponse>
{
    private readonly IProductCategoryRepository _productCategoryRepository;

    public GetProductCategoryByIdQueryHandler(IProductCategoryRepository productCategoryRepository)
    {
        _productCategoryRepository = productCategoryRepository;
    }
    public async Task<Result<ProductCategoryResponse>> Handle(GetProductCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var productCategory = await _productCategoryRepository.GetByIdAsync(request.id, cancellationToken);

        var productCategoryResponse = new ProductCategoryResponse()
        {
            Id = productCategory.Id,
            Name = productCategory.Name,
            Code = productCategory.Code
        };

        return productCategoryResponse;
    }
}
