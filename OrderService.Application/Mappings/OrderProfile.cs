using AutoMapper;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<CreateOrderDto, Order>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
        .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => OrderStatus.Pending))
        .ForMember(dest => dest.Created, opt => opt.MapFrom(_ => DateTime.UtcNow));


        CreateMap<Order, OrderResponseDto>();
    }
}