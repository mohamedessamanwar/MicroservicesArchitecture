using AutoMapper;
using Payment.Application.DTOs;
using Payment.Core.Entities;

namespace Payment.Application.Mappings;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<CreatePaymentDto, Core.Entities.Payment>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => PaymentStatus.Pending))
      .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<Core.Entities.Payment, PaymentResponseDto>();
    }
}