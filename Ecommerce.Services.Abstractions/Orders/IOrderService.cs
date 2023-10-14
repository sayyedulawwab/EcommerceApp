using Ecommerce.Models.EntityModels;
using Ecommerce.Services.Abstractions.Base;

namespace Ecommerce.Services.Abstractions.Orders
{
    public interface IOrderService : IService<Order>
    {
        bool Add(Order order);
        bool Update(Order order);
        bool Delete(Order order);
        Order GetById(int id);
        ICollection<Order> GetAll();
        Order PlaceOrderFromCart(int userId);
    }
}
