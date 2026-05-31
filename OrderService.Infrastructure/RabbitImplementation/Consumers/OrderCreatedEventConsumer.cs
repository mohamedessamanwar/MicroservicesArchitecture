using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.RabbitImplementation.Consumers;

public sealed class OrderCreatedEventConsumer : IEventConsumer
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(AppDbContext dbContext, ILogger<OrderCreatedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public string EventType => nameof(OrderCreatedEvent);

    public async Task ConsumeAsync(JsonElement message, CancellationToken cancellationToken = default)
    {
        var payload = message.GetRawText();
        var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize OrderCreatedEvent payload.");

        _logger.LogInformation(
            "Handling OrderCreatedEvent. EventId={EventId}, OrderId={OrderId}, CustomerId={CustomerId}, TotalAmount={TotalAmount}",
            @event.Id,
            @event.OrderId,
            @event.CustomerId,
            @event.TotalAmount);

        var order = await _dbContext.Orders.FindAsync([@event.OrderId], cancellationToken);

        if (order is null)
        {
            order = new Order
            {
                Id = @event.OrderId,
                CustomerId = @event.CustomerId,
                TotalAmount = @event.TotalAmount,
                Status = OrderStatus.Pending,
                Created = DateTime.UtcNow
            };

            await _dbContext.Orders.AddAsync(order, cancellationToken);

            _logger.LogInformation(
                "Created new order projection from OrderCreatedEvent. OrderId={OrderId}",
                @event.OrderId);
            return;
        }

        order.CustomerId = @event.CustomerId;
        order.TotalAmount = @event.TotalAmount;
        order.Status = OrderStatus.Pending;
        order.Modified = DateTime.UtcNow;

        _logger.LogInformation(
            "Updated existing order projection from OrderCreatedEvent. OrderId={OrderId}",
            @event.OrderId);
    }
}

