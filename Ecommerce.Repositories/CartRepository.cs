
using Ecommerce.Data;
using Ecommerce.Models.EntityModels;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Repositories
{
    public class CartRepository : EFCoreBaseRepository<Cart>, ICartRepository
    {
        private readonly EcommerceEFDbContext _db;
        public CartRepository(EcommerceEFDbContext db) : base(db)
        {
            _db = db;
        }

        public Cart GetCartByUserId(int userId)
        {
            return _db.Carts.Include(cart => cart.CartItems).ThenInclude(cartItem => cartItem.Product).FirstOrDefault(cart => cart.UserID == userId);
        }

        public Cart CreateCartForUser(int userId)
        {
            var newCart = new Cart { UserID = userId };
            _db.Carts.Add(newCart);
            _db.SaveChanges();

            return newCart;
        }

        public bool AddCartItem(CartItem cartItem)
        {
            if (!_db.Carts.Any(c => c.CartID == cartItem.CartID))
            {
                throw new InvalidOperationException("Cart does not exist.");
            }

            _db.CartItems.Add(cartItem);
            return _db.SaveChanges() > 0;
        }

        public bool UpdateCartItem(CartItem cartItem)
        {
            
            _db.CartItems.Update(cartItem);
            return _db.SaveChanges() > 0;
        }

        public bool DeleteCartItem(CartItem cartItem)
        {
           _db.CartItems.Remove(cartItem);
            return _db.SaveChanges() > 0;
        }

        public bool ClearCart(Cart cart)
        {
          cart.CartItems.Clear();
          return _db.SaveChanges() > 0;
        }
    }
}
