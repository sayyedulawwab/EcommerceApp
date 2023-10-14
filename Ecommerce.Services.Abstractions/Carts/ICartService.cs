using Ecommerce.Models.EntityModels;
using Ecommerce.Services.Abstractions.Base;

namespace Ecommerce.Services.Abstractions.Carts
{
    public interface ICartService
    {
        bool AddCartItem(CartItem cartItem);
        bool UpdateCartItem(CartItem cartItem);
        bool DeleteCartItem(CartItem cartItem);
        Cart GetCartByUserId(int userId);
        Cart CreateCartForUser(int userId);
        bool ClearCart(int userId);
    }
}
