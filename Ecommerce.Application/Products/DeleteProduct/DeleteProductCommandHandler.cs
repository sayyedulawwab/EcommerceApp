using Ecommerce.Application.Abstractions.Caching;
using Ecommerce.Application.Abstractions.Data;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Products.DeleteProduct;
internal sealed class DeleteProductCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService)
    : ICommandHandler<DeleteProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await dbContext.Products.FirstOrDefaultAsync(product => product.Id == new ProductId(request.Id), cancellationToken);

        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        if (await HasOrdersAsync(product.Id, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.HasOrders);
        }

        dbContext.Products.Remove(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // An order placed between the check and the delete hits the foreign key. Report it as the same conflict.
            if (await HasOrdersAsync(product.Id, cancellationToken))
            {
                return Result.Failure<Guid>(ProductErrors.HasOrders);
            }

            throw;
        }

        await cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return product.Id.Value;
    }

    private Task<bool> HasOrdersAsync(ProductId productId, CancellationToken cancellationToken)
    {
        return dbContext.Orders.AnyAsync(order => order.OrderItems.Any(item => item.ProductId == productId), cancellationToken);
    }
}
