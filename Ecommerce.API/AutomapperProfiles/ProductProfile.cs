using AutoMapper;
using Ecommerce.API.Models;
using Ecommerce.Models.EntityModels;

namespace Ecommerce.API.AutomapperProfiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductCreateDTO, Product>();
            CreateMap<ProductEditDTO, Product>();
            CreateMap<Product, ProductViewDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.ProductCategoryID, opt => opt.MapFrom(src => src.ProductCategory.ProductCategoryID));

        }
    }
}
