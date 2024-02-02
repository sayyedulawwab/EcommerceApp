using Ecommerce.Domain.Orders;

namespace Ecommerce.Infrastructure.Repositories;
internal sealed class OrderRepository : Repository<Order, OrderId>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}
