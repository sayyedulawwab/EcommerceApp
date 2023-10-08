
using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Repositories
{
    public class OrderRepository : EFCoreBaseRepository<Order>, IOrderRepository
    {
        private readonly EcommerceEFDbContext _db;
        public OrderRepository(EcommerceEFDbContext db) : base(db)
        {
            _db = db;
        }

        public bool Add(Order order)
        {
            _db.Orders.Add(order);
            return _db.SaveChanges() > 0;
        }

        public bool Update(Order order)
        {
            _db.Orders.Update(order);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(Order order)
        {
            _db.Orders.Remove(order);
            return _db.SaveChanges() > 0;
        }
        public Order GetById(int id)
        {
            return _db.Orders.Include(order => order.OrderDetails).FirstOrDefault(order => order.OrderID == id);

        }
        public ICollection<Order> GetAll()
        {
            return _db.Orders.Include(order => order.OrderDetails).ToList();
        }
    }
}
