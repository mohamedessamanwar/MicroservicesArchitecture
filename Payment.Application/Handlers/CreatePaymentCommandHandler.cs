using AutoMapper;
using MediatR;
using Payment.Application.Common;
using Payment.Application.Commands;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Handlers;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CommandResult<PaymentResponseDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public CreatePaymentCommandHandler(IPaymentRepository paymentRepository, IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<CommandResult<PaymentResponseDto>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = _mapper.Map<Core.Entities.Payment>(request.Dto);
        var created = await _paymentRepository.AddAsync(payment, cancellationToken);
        var responseDto = _mapper.Map<PaymentResponseDto>(created);

        return CommandResult<PaymentResponseDto>.Ok(responseDto, "Payment created successfully");
    }
}