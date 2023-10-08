using Ecommerce.Models.EntityModels;
using AutoMapper;
using Ecommerce.API.Models;

namespace Ecommerce.API.AutomapperProfiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile() {

            CreateMap<OrderDetailDTO, OrderDetail>();
            CreateMap<OrderDetail, OrderDetailDTO>();

            CreateMap<OrderCreateDTO, Order>()
                .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.Products));

            
    
            CreateMap<OrderEditDTO, Order>();
            CreateMap<Order, OrderViewDTO>()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.OrderDetails));

        }
    }
}
