using AutoMapper;
using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;

namespace Ecommerce.API.AutomapperProfiles
{
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile() {

            CreateMap<ProductCategoryCreateVM, ProductCategory>();
            CreateMap<ProductCategoryEditVM, ProductCategory>();
            CreateMap<ProductCategory, ProductCategoryViewVM>(); 
        }
        
            
    }
}
