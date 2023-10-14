using Ecommerce.Models.EntityModels;
using AutoMapper;
using Ecommerce.API.Models;

namespace Ecommerce.API.AutomapperProfiles
{
    public class CartProfile : Profile
    {
        public CartProfile() {

            CreateMap<CartItemDTO, CartItem>();
            CreateMap<CartItem, CartItemDTO>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

            CreateMap<Cart, CartViewDTO>()
                .ForMember(dest => dest.CartItems, opt => opt.MapFrom(src => src.CartItems));

   

        }
    }
}
