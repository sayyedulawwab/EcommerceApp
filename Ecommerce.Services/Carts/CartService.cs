using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Services.Abstractions.Carts;


namespace Ecommerce.Services.Carts
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository repository)
        {
            _cartRepository = repository;
        }

        public Cart GetCartByUserId(int userId)
        {
            return _cartRepository.GetCartByUserId(userId);
        }

        public Cart CreateCartForUser(int userId)
        {
            return _cartRepository.CreateCartForUser(userId);
        }

        public bool AddCartItem(CartItem cartItem)
        {
            return _cartRepository.AddCartItem(cartItem);
        }

        public bool UpdateCartItem(CartItem cartItem)
        {

            return _cartRepository.UpdateCartItem(cartItem);
        }

        public bool DeleteCartItem(CartItem cartItem)
        {
            return _cartRepository.AddCartItem(cartItem);
        }


        public bool ClearCart(int userId)
        {
            var cart = GetCartByUserId(userId);
            if (cart != null)
            {
                return _cartRepository.ClearCart(cart);
            }

            return false;
        }


    }
}
