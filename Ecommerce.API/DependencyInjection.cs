using Ecommerce.API.Common.Errors;
using Ecommerce.API.Mappings;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Ecommerce.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
           
            //builder.Services.AddControllers(options => options.Filters.Add<ErrorHandlingFilterAttribute>());
            services.AddControllers();
            services.AddSingleton<ProblemDetailsFactory, EcommerceProblemDetailsFactory>();
            services.AddMappings();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }
    }
}
