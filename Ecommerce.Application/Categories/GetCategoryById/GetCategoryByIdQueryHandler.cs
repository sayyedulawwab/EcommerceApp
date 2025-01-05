using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.GetCategoryById;
internal sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly ICategoryRepository _productCategoryRepository;

    public GetCategoryByIdQueryHandler(ICategoryRepository productCategoryRepository)
    {
        _productCategoryRepository = productCategoryRepository;
    }
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        Category? productCategory = await _productCategoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<CategoryResponse>(CategoryErrors.NotFound);
        }

        var productCategoryResponse = new CategoryResponse()
        {
            Id = productCategory.Id.Value,
            Name = productCategory.Name.Value,
            Code = productCategory.Code.Value
        };

        return productCategoryResponse;
    }
}
