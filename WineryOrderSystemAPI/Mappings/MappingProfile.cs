using AutoMapper;
using WineryOrderSystemAPI.Models;
using WineryOrderSystemAPI.DTOs;

namespace WineryOrderSystemAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Auth
            CreateMap<User, AuthResponseDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : "Customer"))
                .ForMember(dest => dest.Token, opt => opt.Ignore());

            // Wine
            CreateMap<Wine, WineDto>();
            CreateMap<CreateWineDto, Wine>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // User / Customer
            CreateMap<User, PublicCustomerDto>();
            CreateMap<User, AdminCustomerDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty));
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // Orders
            CreateMap<Order, UserOrderDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

            CreateMap<Order, OrderReadDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Unknown"))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

            CreateMap<Order, AdminOrderDto>()
                .ForMember(dest => dest.CostumerId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty))
                .ForMember(dest => dest.DeliveryAddress, opt => opt.MapFrom(src => src.User != null ? src.User.Address : string.Empty))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

            CreateMap<OrderItem, OrderItemReadDto>()
                .ForMember(dest => dest.WineName, opt => opt.MapFrom(src => src.Product != null ? src.Product.WineName : "Unknown"));

            // Cart
            CreateMap<Cart, CartReadDto>()
                .ForMember(dest => dest.SubTotal, opt => opt.MapFrom(src => src.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice)))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.CartItems));

            CreateMap<CartItem, CartItemReadDto>()
                .ForMember(dest => dest.WineName, opt => opt.MapFrom(src => src.Wine != null ? src.Wine.WineName : "Unknown"))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Wine != null ? src.Wine.Category : null))
                .ForMember(dest => dest.AvailableStock, opt => opt.MapFrom(src => src.Wine != null ? src.Wine.StockQuantity : 0));

            // Payment
            CreateMap<Payment, PaymentReadDto>();

            // Chatbot / FAQ
            CreateMap<FaqCreateDto, Faq>();
        }
    }
}