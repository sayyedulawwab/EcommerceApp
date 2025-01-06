using Ecommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class OrderRepository(ApplicationDbContext dbContext)
    : Repository<Order, OrderId>(dbContext), IOrderRepository
{
    public override async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Order>().Include(order => order.OrderItems).ThenInclude(orderItem => orderItem.Product).ToListAsync(cancellationToken);
    }
}
