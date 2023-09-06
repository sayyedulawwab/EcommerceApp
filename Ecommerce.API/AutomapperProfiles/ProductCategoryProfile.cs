using AutoMapper;
using Ecommerce.API.Models;
using Ecommerce.Models.EntityModels;

namespace Ecommerce.API.AutomapperProfiles
{
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile() {

            CreateMap<ProductCategoryCreateDTO, ProductCategory>();
            CreateMap<ProductCategoryEditDTO, ProductCategory>();
            CreateMap<ProductCategory, ProductCategoryViewDTO>(); 
        }
        
            
    }
}
