using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;

namespace Ecommerce.Application.Products.EditProduct;
internal sealed class EditProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ICacheService cacheService)
    : ICommandHandler<EditProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetByIdAsync(new ProductId(request.Id), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        product = Product.Update(
            product,
            new ProductName(request.Name),
            new ProductDescription(request.Description),
            new Money(request.PriceAmount,
            Currency.Create(request.PriceCurrency)),
            request.Quantity,
            new CategoryId(request.CategoryId),
            dateTimeProvider.UtcNow);

        productRepository.Update(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return product.Id.Value;
    }
}
