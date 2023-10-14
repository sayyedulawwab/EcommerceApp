using Ecommerce.Models.EntityModels;
using Ecommerce.Models.UtilityModels;
using Ecommerce.Repositories;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Services.Abstractions.Carts;
using Ecommerce.Services.Abstractions.Orders;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Base;
using Microsoft.EntityFrameworkCore;
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
        private readonly ICartService _cartService;

        public OrderService(IOrderRepository repository, ICartService cartService) : base(repository)
        {
            _orderRepository = repository;
            _cartService = cartService;
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

        public Order PlaceOrderFromCart(int userId)
        {
            // Retrieve the cart for the user
            var cart = _cartService.GetCartByUserId(userId);

            if (cart == null || !cart.CartItems.Any())
            {
                throw new InvalidOperationException("The cart is empty");
            }

            // Create a new order object
            var order = new Order
            {
                UserID = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                OrderDetails = new List<OrderDetail>()
            };

        

            // For each cart item, create an order detail
            foreach (var cartItem in cart.CartItems)
            {
                var orderDetail = new OrderDetail
                {
                    ProductID = cartItem.ProductID,
                    Quantity = cartItem.Quantity,

                    
                };

                order.TotalPrice += cartItem.Product.Price * orderDetail.Quantity;

                order.OrderDetails.Add(orderDetail);
            }

            // Add order to the database (this doesn't commit it yet)
            bool isSuccess = _orderRepository.Add(order);

            if (isSuccess)
            {
                // Clear the cart
                bool isCartClearSuccess = _cartService.ClearCart(cart.UserID);

                return order;
            }
            else {

                return null;
            }

            
        }

    }
}
