using AutoMapper;
using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;

namespace Ecommerce.API.AutomapperProfiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductCreateVM, Product>();
            CreateMap<ProductEditVM, Product>();
            CreateMap<Product, ProductViewVM>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.ProductCategoryName, opt => opt.MapFrom(src => src.ProductCategory.Name)); 

        }
    }
}
