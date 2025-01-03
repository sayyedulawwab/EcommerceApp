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
        ProductCategory? productCategory = await _productCategoryRepository.GetByIdAsync(new ProductCategoryId(request.Id), cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<ProductCategoryResponse>(ProductCategoryErrors.NotFound);
        }

        var productCategoryResponse = new ProductCategoryResponse()
        {
            Id = productCategory.Id.Value,
            Name = productCategory.Name.Value,
            Code = productCategory.Code.Value
        };

        return productCategoryResponse;
    }
}
