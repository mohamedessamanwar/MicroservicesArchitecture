using AutoMapper;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, CommandResult<IEnumerable<OrderResponseDto>>>
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IMapper _mapper;

    public GetOrdersQueryHandler(IRepository<Order> orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<CommandResult<IEnumerable<OrderResponseDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _orderRepository.ListAsync(cancellationToken);
            var result = _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
            
            return CommandResult<IEnumerable<OrderResponseDto>>.Ok(result, "Orders retrieved successfully");
        }
        catch (Exception ex)
        {
            return CommandResult<IEnumerable<OrderResponseDto>>.Fail($"Failed to retrieve orders: {ex.Message}");
        }
    }
}
