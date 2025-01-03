using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.ProductCategories.EditProductCategory;
internal sealed class EditProductCategoryCommandHandler : ICommandHandler<EditProductCategoryCommand, Guid>
{
    private readonly IProductCategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditProductCategoryCommandHandler(IProductCategoryRepository productCategoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(EditProductCategoryCommand request, CancellationToken cancellationToken)
    {
        ProductCategory? productCategory = await _productCategoryRepository.GetByIdAsync(new ProductCategoryId(request.Id), cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<Guid>(ProductCategoryErrors.NotFound);
        }

        productCategory = ProductCategory.Update(productCategory, new CategoryName(request.Name), new CategoryCode(request.Code), _dateTimeProvider.UtcNow);

        _productCategoryRepository.Update(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id.Value;

    }
}
