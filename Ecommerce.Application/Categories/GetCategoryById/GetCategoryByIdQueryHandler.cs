using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Categories;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.GetCategoryById;
internal sealed class GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<CategoryResponse>(CategoryErrors.NotFound);
        }

        var categoryResponse = new CategoryResponse()
        {
            Id = category.Id.Value,
            Name = category.Name.Value,
            Code = category.Code.Value
        };

        return categoryResponse;
    }
}
