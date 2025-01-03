using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.ProductCategories.DeleteProductCategory;
internal sealed class DeleteProductCategoryCommandHandler : ICommandHandler<DeleteProductCategoryCommand, Guid>
{
    private readonly IProductCategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCategoryCommandHandler(IProductCategoryRepository productCategoryRepository, IUnitOfWork unitOfWork)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(DeleteProductCategoryCommand request, CancellationToken cancellationToken)
    {
        ProductCategory? productCategory = await _productCategoryRepository.GetByIdAsync(new ProductCategoryId(request.Id), cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<Guid>(ProductCategoryErrors.NotFound);
        }

        _productCategoryRepository.Remove(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id.Value;

    }
}
