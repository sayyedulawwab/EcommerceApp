using Ecommerce.Data;
using Ecommerce.Repositories;
using Ecommerce.Repositories.Abstractions;
using Ecommerce.Services.Abstractions.Auth;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Auth;
using Ecommerce.Services.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application.Configuraitons
{
    public static class DependencyConfigurations
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddTransient<IProductCategoryRepository, ProductCategoryRepository>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductCategoryService, ProductCategoryService>();
            services.AddTransient<IProductRepository, ProductRepository>();

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IAuthService, AuthService>();

            services.AddDbContext<EcommerceEFDbContext>(options =>
            {
                string connectionString = "Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=True";
                options.UseSqlServer(connectionString);
            });

        }
    }
}
