using Ecommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class OrderRepository : Repository<Order, OrderId>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Order>().Include(order => order.OrderItems).ThenInclude(orderItem => orderItem.Product).ToListAsync(cancellationToken);
    }
}
