using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.EditCategory;
internal sealed class EditCategoryCommandHandler : ICommandHandler<EditCategoryCommand, Guid>
{
    private readonly ICategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditCategoryCommandHandler(ICategoryRepository productCategoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? productCategory = await _productCategoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        productCategory = Category.Update(productCategory, new CategoryName(request.Name), new CategoryCode(request.Code), _dateTimeProvider.UtcNow);

        _productCategoryRepository.Update(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id.Value;

    }
}
