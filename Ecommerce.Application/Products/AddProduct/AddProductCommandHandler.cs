using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Products.AddProduct;
internal sealed class AddProductCommandHandler : ICommandHandler<AddProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(new ProductName(request.name), new ProductDescription(request.description), new Money(request.priceAmount, Currency.Create(request.priceCurrency)), request.quantity, new ProductCategoryId(request.productCategoryId), _dateTimeProvider.UtcNow);

        _productRepository.Add(product);

        await _unitOfWork.SaveChangesAsync();

        return product.Id.Value;

    }
}
