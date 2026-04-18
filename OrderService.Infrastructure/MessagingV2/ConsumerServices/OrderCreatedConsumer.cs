using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Events;

namespace OrderService.Infrastructure.MessagingV2.ConsumerServices;

/// <summary>
/// Domain handler for <see cref="OrderCreatedEvent"/>; resolved by <see cref="OrderCreatedConsumerJob"/> via naming convention (EventType → *Consumer).
/// </summary>
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task ConsumeAsync(OrderCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "OrderCreated consumed: OrderId={OrderId}, CustomerId={CustomerId}, Amount={Amount}",
            message.OrderId, message.CustomerId, message.TotalAmount);

        return Task.CompletedTask;
    }
}
