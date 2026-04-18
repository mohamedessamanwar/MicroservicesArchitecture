using AutoMapper;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CommandResult<OrderResponseDto>>
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IMapper _mapper;

    public CreateOrderCommandHandler(IRepository<Order> orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<CommandResult<OrderResponseDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = _mapper.Map<Order>(request.Dto);
            var created = await _orderRepository.AddAsync(order, cancellationToken);
            var responseDto = _mapper.Map<OrderResponseDto>(created);

            return CommandResult<OrderResponseDto>.Ok(responseDto, "Order created successfully");
        }
        catch (Exception ex)
        {
            return CommandResult<OrderResponseDto>.Fail($"Failed to create order: {ex.Message}");
        }
    }
}