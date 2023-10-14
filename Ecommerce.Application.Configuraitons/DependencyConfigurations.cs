using Ecommerce.Data;
using Ecommerce.Repositories;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Services.Abstractions.Auth;
using Ecommerce.Services.Abstractions.Carts;
using Ecommerce.Services.Abstractions.Orders;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Auth;
using Ecommerce.Services.Carts;
using Ecommerce.Services.Orders;
using Ecommerce.Services.Products;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Ecommerce.Application.Configuraitons
{
    public static class DependencyConfigurations
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddTransient<IProductCategoryRepository, ProductCategoryRepository>();
            services.AddTransient<IProductCategoryService, ProductCategoryService>();

            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductRepository, ProductRepository>();

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IAuthService, AuthService>();

            services.AddTransient<IOrderRepository, OrderRepository>();
            services.AddTransient<IOrderService, OrderService>();

            services.AddTransient<ICartRepository, CartRepository>();
            services.AddTransient<ICartService, CartService>();

            services.AddDbContext<EcommerceEFDbContext>(options =>
            {
                string connectionString = "Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=True";
                options.UseSqlServer(connectionString);
            });

        }
    }
}
