using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Categories.DeleteCategory;
internal sealed class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, Guid>
{
    private readonly ICategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(ICategoryRepository productCategoryRepository, IUnitOfWork unitOfWork)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? productCategory = await _productCategoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        _productCategoryRepository.Remove(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id.Value;

    }
}
