using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Products.EditProduct;
internal sealed class EditProductCommandHandler : ICommandHandler<EditProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EditProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(new ProductId(request.id));

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        product = Product.Update(product, new ProductName(request.name), new ProductDescription(request.description), new Money(request.priceAmount, Currency.Create(request.priceCurrency)), request.quantity, new ProductCategoryId(request.productCategoryId), _dateTimeProvider.UtcNow);

        _productRepository.Update(product);

        await _unitOfWork.SaveChangesAsync();

        return product.Id.Value;

    }
}
