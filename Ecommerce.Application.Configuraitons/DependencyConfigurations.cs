using Ecommerce.Data;
using Ecommerce.Repositories;
using Ecommerce.Services.Abstractions.Products;
using Ecommerce.Services.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            services.AddDbContext<EcommerceDbContext>(options =>
            {
                string connectionString = "Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=True";
                options.UseSqlServer(connectionString);
                
            });
        }
    }
}
