using System.Text.Json;
using AutoMapper;
using Micro.Shared.Http.Clients.Payment;
using Micro.Shared.Http.Clients.Payment.DTOs;
using Micro.Shared.Http.Clients.Product;
using OrderService.Application.Common;
using OrderService.Application.Constants;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Application.UseCases;

public class CreateOrderUseCase : ICreateOrderUseCase
{
    private readonly IOrderDbContext _dbContext; 
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Saga> _sagaRepository;
    private readonly IProductServiceClient _productServiceClient;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IMapper _mapper;

    public CreateOrderUseCase(
        IOrderDbContext dbContext,
        IRepository<Order> orderRepository,
        IRepository<Saga> sagaRepository,
        IProductServiceClient productServiceClient,
        IPaymentServiceClient paymentServiceClient,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
        _sagaRepository = sagaRepository;
        _productServiceClient = productServiceClient;
        _paymentServiceClient = paymentServiceClient;
        _mapper = mapper;
    }

    public async Task<CommandResult<OrderResponseDto>> ExecuteAsync(CreateOrderDto dto, string idempotencyKey, CancellationToken cancellationToken)
    {
        // 1. Idempotency Check & Initial Order Creation within a single atomic transaction
        var (isDuplicate, cachedResponse, order, saga) = await CreateInitialOrderAtomicAsync(dto, idempotencyKey, cancellationToken);

        if (isDuplicate)
        {
            if (cachedResponse != null)
            {
                // Request already processed successfully
                return CommandResult<OrderResponseDto>.Ok(cachedResponse, "Order completed successfully via Saga (Idempotent response).");
            }
            
            // Request is currently processing or failed previously
            return CommandResult<OrderResponseDto>.Fail("A request with this Idempotency Key is already being processed (409 Conflict).");
        }

        var itemsToReserve = dto.Items.Select(i => new ProductQuantityDto(i.ProductId, i.Quantity)).ToList();
        var reserveStep = saga.Steps.First(s => s.StepName == SagaConstants.Steps.ReserveInventory);

        // 2. Reserve Inventory
        var reserveResult = await ReserveInventoryAsync(saga, reserveStep, order, itemsToReserve, cancellationToken);
        if (!reserveResult)
        {
            return CommandResult<OrderResponseDto>.Fail("Failed to reserve inventory.");
        }

        // 3. Charge Payment
        var chargeStep = await PrepareChargePaymentStepAsync(saga, order, cancellationToken);
        var paymentResult = await ChargePaymentAsync(saga, chargeStep, order, itemsToReserve, reserveStep, cancellationToken);
        if (!paymentResult)
        {
            return CommandResult<OrderResponseDto>.Fail("Failed to process payment. Compensation triggered.");
        }

        // 4. Complete Saga and update Idempotency Record
        var responseDto = await CompleteSagaAsync(saga, order, idempotencyKey, cancellationToken);
        
        return CommandResult<OrderResponseDto>.Ok(responseDto, "Order completed successfully via Saga.");
    }

    private async Task<(bool IsDuplicate, OrderResponseDto? CachedResponse, Order Order, Saga Saga)> CreateInitialOrderAtomicAsync(
        CreateOrderDto dto, 
        string idempotencyKey, 
        CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check if IdempotencyKey exists
            var existingRecord = await _dbContext.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existingRecord != null)
            {
                OrderResponseDto? cachedResponse = null;
                if (!string.IsNullOrEmpty(existingRecord.ResponseJson))
                {
                    cachedResponse = JsonSerializer.Deserialize<OrderResponseDto>(existingRecord.ResponseJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                // It's a duplicate request
                return (true, cachedResponse, null!, null!);
            }

            // Create Idempotency Record
            var idempotencyRecord = new IdempotencyRecord
            {
                IdempotencyKey = idempotencyKey,
                RequestName = "CreateOrder",
                CreatedAtUtc = DateTime.UtcNow
            };
            await _dbContext.IdempotencyRecords.AddAsync(idempotencyRecord, cancellationToken);

            // Create Order
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                TotalAmount = dto.TotalAmount,
                Status = OrderStatus.Pending,
                OrderDetails = dto.Items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
            await _dbContext.Orders.AddAsync(order, cancellationToken);

            // Create Saga
            var saga = new Saga
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                Type = SagaConstants.Types.CreateOrderSaga,
                BusinessId = order.Id.ToString(), // Will be updated after SaveChanges if int, but it's Guid so we have it
                Status = SagaStatus.Executing,
                CurrentStep = SagaConstants.Steps.ReserveInventory,
            };
            
            var reserveStep = new SagaStep
            {
                StepName = SagaConstants.Steps.ReserveInventory,
                Status = SagaStepStatus.Executing,
                Attempts = 1,
                Payload = JsonSerializer.Serialize(dto.Items.Select(i => new { i.ProductId, i.Quantity }).ToList())
            };
            saga.Steps.Add(reserveStep);
            await _dbContext.Sagas.AddAsync(saga, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (false, null, order, saga);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            // Unique constraint violation on IdempotencyKey means another thread beat us to it
            return (true, null, null!, null!);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> ReserveInventoryAsync(
        Saga saga, 
        SagaStep reserveStep, 
        Order order, 
        List<ProductQuantityDto> itemsToReserve, 
        CancellationToken cancellationToken)
    {
        var reserveResult = await _productServiceClient.DecreaseStockBulkAsync(
            itemsToReserve, 
            idempotencyKey: $"{saga.CorrelationId}-reserve", 
            cancellationToken: cancellationToken);
        
        if (!reserveResult.Success)
        {
            reserveStep.Status = SagaStepStatus.Failed;
            reserveStep.ErrorMessage = reserveResult.ErrorMessage ?? "Failed to reserve inventory";
            saga.Status = SagaStatus.Failed;
            order.Status = OrderStatus.Cancelled;
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }
        
        reserveStep.Status = SagaStepStatus.Completed;
        return true;
    }

    private async Task<SagaStep> PrepareChargePaymentStepAsync(Saga saga, Order order, CancellationToken cancellationToken)
    {
        saga.CurrentStep = SagaConstants.Steps.ChargePayment;
        var chargeStep = new SagaStep
        {
            StepName = SagaConstants.Steps.ChargePayment,
            Status = SagaStepStatus.Executing,
            Attempts = 1,
            Payload = JsonSerializer.Serialize(new { order.Id, order.TotalAmount })
        };
        saga.Steps.Add(chargeStep);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return chargeStep;
    }

    private async Task<bool> ChargePaymentAsync(
        Saga saga, 
        SagaStep chargeStep, 
        Order order, 
        List<ProductQuantityDto> itemsToReserve, 
        SagaStep reserveStep, 
        CancellationToken cancellationToken)
    {
        var paymentResult = await _paymentServiceClient.CreatePaymentAsync(
            new CreatePaymentRequest(order.Id, order.TotalAmount),
            idempotencyKey: $"{saga.CorrelationId}-charge",
            cancellationToken: cancellationToken);
            
        if (!paymentResult.Success)
        {
            chargeStep.Status = SagaStepStatus.Failed;
            chargeStep.ErrorMessage = paymentResult.ErrorMessage ?? "Failed to process payment";
            
            saga.Status = SagaStatus.Compensating;
            
            // Trigger compensation logic
            reserveStep.CompensationStatus = CompensationStatus.Executing;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var compResult = await _productServiceClient.IncreaseStockBulkAsync(
                itemsToReserve, 
                idempotencyKey: $"{saga.CorrelationId}-reserve-comp", 
                cancellationToken: cancellationToken);
            
            if (compResult.Success)
            {
                reserveStep.CompensationStatus = CompensationStatus.Completed;
                saga.Status = SagaStatus.Compensated;
            }
            else
            {
                reserveStep.CompensationStatus = CompensationStatus.Failed;
                // Leave saga in Compensating to be picked up by the background worker
            }
            
            order.Status = OrderStatus.Cancelled;
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return false;
        }
        
        chargeStep.Status = SagaStepStatus.Completed;
        return true;
    }

    private async Task<OrderResponseDto> CompleteSagaAsync(Saga saga, Order order, string idempotencyKey, CancellationToken cancellationToken)
    {
        saga.Status = SagaStatus.Completed;
        saga.CurrentStep = SagaConstants.Steps.Completed;
        order.Status = OrderStatus.Paid;
        
        var responseDto = _mapper.Map<OrderResponseDto>(order);

        // Update Idempotency Record with the response JSON
        var idempotencyRecord = await _dbContext.IdempotencyRecords.FindAsync(new object[] { idempotencyKey }, cancellationToken);
        if (idempotencyRecord != null)
        {
            idempotencyRecord.ResponseJson = JsonSerializer.Serialize(responseDto);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return responseDto;
    }
}
