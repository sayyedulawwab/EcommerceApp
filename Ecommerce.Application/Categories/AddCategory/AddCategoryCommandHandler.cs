using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;

namespace Ecommerce.Application.Categories.AddCategory;
internal sealed class AddCategoryCommandHandler : ICommandHandler<AddCategoryCommand, Guid>
{
    private readonly ICategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddCategoryCommandHandler(ICategoryRepository productCategoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var productCategory = Category.Create(new CategoryName(request.Name), new CategoryCode(request.Code), _dateTimeProvider.UtcNow);

        _productCategoryRepository.Add(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id.Value;

    }
}
