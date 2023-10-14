using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions.Base;

namespace Ecommerce.Repositories.Abstractions
{
    public interface IOrderRepository : IRepository<Order>
    {
        bool Add(Order order);
        bool Update(Order order);
        bool Delete(Order order);
        Order GetById(int id);
        ICollection<Order> GetAll();
    }
}
