using Ecommerce.Models.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Data
{
    public class EcommerceEFDbContext : DbContext
    {
        public EcommerceEFDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetail { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
    }
}