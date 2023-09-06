using AutoMapper;
using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;

namespace Ecommerce.API.AutomapperProfiles
{
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile() {

            CreateMap<ProductCategoryCreateVM, ProductCategory>()
                //.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                //.ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code));

            CreateMap<ProductCategoryEditVM, ProductCategory>();
                //.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                //.ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code));
        }
        
            
    }
}
