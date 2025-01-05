using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Application.Products.AddProduct;
internal sealed class AddProductCommandHandler : ICommandHandler<AddProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;

    public AddProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider, ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
    }

    public async Task<Result<Guid>> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(new ProductName(request.Name), new ProductDescription(request.Description), new Money(request.PriceAmount, Currency.Create(request.PriceCurrency)), request.Quantity, new CategoryId(request.ProductCategoryId), _dateTimeProvider.UtcNow);

        _productRepository.Add(product);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return product.Id.Value;

    }
}
