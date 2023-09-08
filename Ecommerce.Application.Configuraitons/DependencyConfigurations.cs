using Ecommerce.Data;
using Ecommerce.Repositories;
using Ecommerce.Services.Abstractions.Products;
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

            services.AddDbContext<EcommerceEFDbContext>(options =>
            {
                string connectionString = "Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=True";
                options.UseSqlServer(connectionString);
            });

            services.AddTransient(sp => new EcommerceDapperDbContext("Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=True"));
            services.AddTransient<IProductCategoryRepository, DapperProductCategoryRepository>();
            services.AddTransient<IProductRepository, DapperProductRepository>();

        }
    }
}
