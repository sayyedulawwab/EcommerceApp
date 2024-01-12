using Ecommerce.Application.Abstactions.Clock;
using Ecommerce.Application.Abstactions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.Products.AddProductCategory;
internal sealed class AddProductCategoryCommandHandler : ICommandHandler<AddProductCategoryCommand, Guid>
{
    private readonly IProductCategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddProductCategoryCommandHandler(IProductCategoryRepository productCategoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var productCategory = ProductCategory.Create(new CategoryName(request.name), new CategoryCode(request.code), _dateTimeProvider.UtcNow);

        _productCategoryRepository.Add(productCategory);

        await _unitOfWork.SaveChangesAsync();

        return productCategory.Id;

    }
}
