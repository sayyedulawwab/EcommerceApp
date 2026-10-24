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

        bool hasOrders = await dbContext.Orders.AnyAsync(order => order.OrderItems.Any(item => item.ProductId == product.Id), cancellationToken);

        if (hasOrders)
        {
            return Result.Failure<Guid>(ProductErrors.HasOrders);
        }

        dbContext.Products.Remove(product);

        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPrefixAsync("products", cancellationToken);

        return product.Id.Value;
    }
}
