using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Interfaces;
using OrderService.Domain.Events;
using OrderService.Infrastructure.Data;

namespace OrderService.Api.Controllers;

/// <summary>
/// Test-only API: enqueues <see cref="OrderCreatedEvent"/> to the outbox; dispatcher publishes to RabbitMQ; consumer listens on queue order.created.q.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessagingTestController : ControllerBase
{
    private readonly IEventPublisher _eventPublisher;
    private readonly AppDbContext _dbContext;

    public MessagingTestController(IEventPublisher eventPublisher, AppDbContext dbContext)
    {
        _eventPublisher = eventPublisher;
        _dbContext = dbContext;
    }

    public sealed record PublishOrderCreatedRequest(Guid OrderId, Guid CustomerId, decimal TotalAmount);

    /// <summary>
    /// Writes outbox row + saves. Dispatcher sends to exchange billing.exchange, routing key order.created.
    /// Consumer listens on queue: order.created.q (must match OrderCreatedConsumerTopology in infrastructure).
    /// </summary>
    [HttpPost("publish-order-created")]
    public async Task<IActionResult> PublishOrderCreated(
        [FromBody] PublishOrderCreatedRequest request,
        CancellationToken cancellationToken)
    {
        var @event = new OrderCreatedEvent(request.OrderId, request.CustomerId, request.TotalAmount);
        await _eventPublisher.EnqueueAsync(@event, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Outbox row saved. Dispatcher will publish when it runs.",
            consumerQueue = "order.created.q",
            exchange = "billing.exchange",
            routingKey = "order.created"
        });
    }
}
