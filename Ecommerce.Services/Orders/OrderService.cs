using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Services.Abstractions.Orders;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Services.Orders
{
    public class OrderService : BaseService<Order>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository repository) : base(repository)
        {
            _orderRepository = repository;
        }

        public override ICollection<Order> GetAll()
        {
            return _orderRepository.GetAll();
        }

        public Order GetById(int id)
        {
            return _orderRepository.GetById(id);
        }

        public override bool Add(Order entity)
        {
            return _orderRepository.Add(entity);
        }

        public override bool Update(Order entity)
        {
            return _orderRepository.Update(entity);
        }
        public override bool Delete(Order entity)
        {
            return _orderRepository.Delete(entity);
        }
     
    }
}
