using AutoMapper;
using WineryOrderSystemAPI.Models;
using WineryOrderSystemAPI.DTOs;

namespace WineryOrderSystemAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Wine Mappings
            CreateMap<Wine, WineDto>();
            CreateMap<CreateWineDto, Wine>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.Now));

            //Customer / User Mappings
            CreateMap<User, PublicCostumerDto>();
            CreateMap<User, AdminCostumerDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty));
            CreateMap<RegisterCostumerDto, User>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // Order Mapping
            // User view mapping (minimal customer info)
            CreateMap<Order, UserOrderDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

            // Admin view mapping (full customer contact/address info)
            CreateMap<Order, AdminOrderDto>()
                .ForMember(dest => dest.CostumerId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty))
                .ForMember(dest => dest.DeliveryAddress, opt => opt.MapFrom(src => src.User != null ? src.User.Address : string.Empty))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

            // Order line item detail mapping
            CreateMap<OrderItem, OrderItemDetailDto>()
                .ForMember(dest => dest.WineName, opt => opt.MapFrom(src => src.Product != null ? src.Product.WineName : string.Empty));
        }
    }
}