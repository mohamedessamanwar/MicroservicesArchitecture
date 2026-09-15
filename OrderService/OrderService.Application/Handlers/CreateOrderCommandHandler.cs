using AutoMapper;
using MediatR;
using Micro.Shared.Http.Clients.Payment;
using Micro.Shared.Http.Clients.Payment.DTOs;
using Micro.Shared.Http.Clients.Product;
using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Application.UseCases;

namespace OrderService.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CommandResult<OrderResponseDto>>
{
    private readonly ICreateOrderUseCase _useCase;

    public CreateOrderCommandHandler(ICreateOrderUseCase useCase)
    {
        _useCase = useCase;
    }

    public async Task<CommandResult<OrderResponseDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        return await _useCase.ExecuteAsync(request.Dto, request.IdempotencyKey, cancellationToken);
    }
}