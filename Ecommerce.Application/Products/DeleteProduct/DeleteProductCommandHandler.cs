using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;

namespace Ecommerce.Application.Products.DeleteProduct;
internal sealed class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : ICommandHandler<DeleteProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetByIdAsync(new ProductId(request.Id), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        productRepository.Remove(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return product.Id.Value;
    }
}
