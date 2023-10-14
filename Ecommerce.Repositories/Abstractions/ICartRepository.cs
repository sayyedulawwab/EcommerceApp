using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions.Base;

namespace Ecommerce.Repositories.Abstractions
{
    public interface ICartRepository : IRepository<Cart>
    {
        bool AddCartItem(CartItem cartItem);
        bool UpdateCartItem(CartItem cartItem);
        bool DeleteCartItem(CartItem cartItem);
        Cart GetCartByUserId(int userId);
        Cart CreateCartForUser(int userId);
        bool ClearCart(Cart cart);


    }
}
