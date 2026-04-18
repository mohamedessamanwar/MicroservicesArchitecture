using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.MessagingV2.Connections;
using OrderService.Infrastructure.MessagingV2.Inbox;
using OrderService.Infrastructure.MessagingV2.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Infrastructure.MessagingV2.ConsumerServices;

/// <summary>
/// Background consumer job: subscribes to order.created.q (declared by RabbitMqTopologyInitializerHostedService),
/// applies inbox de-duplication, deserializes <see cref="BaseEvent"/> payloads, then dispatches to <see cref="Application.Interfaces.IConsumer{T}"/>
/// implementations in this namespace.
/// 
/// Design decisions:
/// - Waits for RabbitMqTopologyInitializerHostedService to complete before attempting to consume.
/// - Queue, exchange, and bindings are created by topology initializer; consumer only consumes.
/// - Maintains inbox idempotency to prevent duplicate event processing.
/// - Nacks without requeue on processing failure; messages are dead-lettered by RabbitMQ.
/// </summary>
public sealed class OrderCreatedConsumerJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionRegistry _connectionRegistry;
    private readonly TopologyInitializationCoordinator _topologyCoordinator;
    private readonly ILogger<OrderCreatedConsumerJob> _logger;
    private IModel? _channel;

    public OrderCreatedConsumerJob(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionRegistry connectionRegistry,
        TopologyInitializationCoordinator topologyCoordinator,
        ILogger<OrderCreatedConsumerJob> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionRegistry = connectionRegistry;
        _topologyCoordinator = topologyCoordinator;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderCreatedConsumerJob is waiting for topology initialization to complete.");

        try
        {
            // Wait for topology initialization before starting consumption
            await _topologyCoordinator.WaitForInitializationAsync(cancellationToken);

            var connection = _connectionRegistry.GetConnection("BillingBroker");
            _channel = connection.CreateModel();
            
            // Set QoS for consumption: prefetch 5 messages at a time
            _channel.BasicQos(0, prefetchCount: 5, global: false);
            
            _logger.LogInformation(
                "OrderCreatedConsumerJob initialized and ready to consume. Queue='order.created.q'");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Failed to initialize OrderCreatedConsumerJob. " +
                "Either topology initialization failed or RabbitMQ broker is unreachable.");
            throw;
        }
        
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogCritical("RabbitMQ channel is null; consumer job will not run.");
            return;
        }

        try
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var messageIdStr = ea.BasicProperties?.MessageId;
                    if (string.IsNullOrEmpty(messageIdStr) || !Guid.TryParse(messageIdStr, out var messageId))
                    {
                        _logger.LogWarning("Message without valid MessageId; acking. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (await inboxStore.ExistsAsync(messageId, stoppingToken))
                    {
                        _logger.LogInformation("Duplicate message detected; acking. MessageId={MessageId}", messageId);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var baseEvent = DeserializeBaseEvent(messageJson);

                    var executionStrategy = db.Database.CreateExecutionStrategy();
                    await executionStrategy.ExecuteAsync(async () =>
                    {
                        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                        await InvokeConsumerAsync(baseEvent, scope.ServiceProvider, stoppingToken);

                        db.InboxMessages.Add(new InboxMessage
                        {
                            Id = Guid.NewGuid(),
                            MessageId = messageId,
                            EventType = baseEvent.EventType,
                            ProcessedOnUtc = DateTime.UtcNow
                        });

                        await db.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    });

                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Message processed successfully. MessageId={MessageId}, EventType={EventType}", messageId, baseEvent.EventType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consumer job failed; nacking without requeue. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                    try
                    {
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    catch (Exception nackEx)
                     {
                         _logger.LogError(nackEx, "Failed to nack message. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                     }
                 }
             };

            _channel.BasicConsume("order.created.q", autoAck: false, consumer);
            _logger.LogInformation(
                "OrderCreatedConsumerJob started consuming. Queue='order.created.q'");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderCreatedConsumerJob cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error in OrderCreatedConsumerJob. Queue='order.created.q'");
            throw;
        }
    }

    private static BaseEvent DeserializeBaseEvent(string payload)
    {
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(payload);
        if (!jsonElement.TryGetProperty("EventType", out var eventTypeProperty))
            throw new InvalidOperationException("EventType property not found in payload");

        var eventType = eventTypeProperty.GetString();
        const string eventsNamespace = "OrderService.Domain.Events";
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == eventType && t.Namespace == eventsNamespace);

        if (type == null)
            throw new InvalidOperationException($"Event type '{eventType}' not found in namespace '{eventsNamespace}'");

        return (BaseEvent)JsonSerializer.Deserialize(payload, type)!;
    }

    private static async Task InvokeConsumerAsync(BaseEvent baseEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typeName = baseEvent.EventType.Replace("Event", "Consumer");
        const string consumerNamespace = "OrderService.Infrastructure.MessagingV2.ConsumerServices";
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == typeName && t.Namespace == consumerNamespace);

        if (type == null)
            throw new InvalidOperationException($"Consumer type '{typeName}' not found in namespace '{consumerNamespace}'");

        var consumer = ActivatorUtilities.CreateInstance(serviceProvider, type);
        var consumeMethod = type.GetMethod("ConsumeAsync", new[] { baseEvent.GetType(), typeof(CancellationToken) })
            ?? type.GetMethod("ConsumeAsync", new[] { baseEvent.GetType() });

        if (consumeMethod == null)
            throw new InvalidOperationException($"ConsumeAsync not found on consumer type '{typeName}'");

        var args = consumeMethod.GetParameters().Length == 2
            ? new object[] { baseEvent, cancellationToken }
            : new object[] { baseEvent };

        var result = consumeMethod.Invoke(consumer, args);
        if (result is Task t)
            await t;
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing consumer job channel");
        }

        base.Dispose();
    }
}
