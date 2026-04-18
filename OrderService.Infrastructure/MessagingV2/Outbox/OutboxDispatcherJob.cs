using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.MessagingV2.Connections;
using OrderService.Infrastructure.MessagingV2.Topology;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.MessagingV2.Outbox;

/// <summary>
/// Background job: reads pending outbox rows, publishes via pooled channels, updates tracked objects in-memory, saves all in single batch.
/// 
/// Design decisions:
/// - Waits for RabbitMqTopologyInitializerHostedService to complete before attempting to publish.
/// - Exchanges are created by topology initializer; publisher only publishes to pre-declared exchanges.
/// - Failures are handled per-message, not per-provider-group, to maximize throughput.
/// - Messages returned from GetPendingBatchAsync are tracked by DbContext; updates are made to objects in-memory.
/// - Single SaveChangesAsync() call after all messages processed reduces database round trips from 50+ to 1.
/// - Comprehensive structured logging includes outbox ID, MessageId, provider, exchange, routing key, retry count, and failure reason.
/// </summary>
public sealed class OutboxDispatcherJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChannelPool _channelPool;
    private readonly TopologyInitializationCoordinator _topologyCoordinator;
    private readonly ILogger<OutboxDispatcherJob> _logger;

    public OutboxDispatcherJob(
        IServiceScopeFactory scopeFactory,
        IChannelPool channelPool,
        TopologyInitializationCoordinator topologyCoordinator,
        ILogger<OutboxDispatcherJob> logger)
    {
        _scopeFactory = scopeFactory;
        _channelPool = channelPool;
        _topologyCoordinator = topologyCoordinator;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OutboxDispatcherJob is waiting for topology initialization to complete.");

        try
        {
            // Wait for topology initialization before starting dispatch
            await _topologyCoordinator.WaitForInitializationAsync(cancellationToken);

            _logger.LogInformation("OutboxDispatcherJob initialized and ready to dispatch messages.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "OutboxDispatcherJob failed to initialize. " +
                "Either topology initialization failed or RabbitMQ broker is unreachable.");
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            
            // Get tracked messages from database.
            var messages = await outboxStore.GetPendingBatchAsync(50, stoppingToken);

            if (messages.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            // Track outcomes for logging summary.
            int sentCount = 0;
            int failedCount = 0;

            foreach (var providerGroup in messages.GroupBy(x => x.ProviderName))
            {
                var channel = _channelPool.Rent(providerGroup.Key);
                try
                {
                    foreach (var message in providerGroup)
                    {
                        try
                        {
                            // Publish message; assume exchange is pre-provisioned.
                            var body = Encoding.UTF8.GetBytes(message.Payload);
                            var props = channel.CreateBasicProperties();
                            props.MessageId = message.MessageId.ToString();
                            props.Persistent = true;
                            
                            channel.BasicPublish(
                                message.ExchangeName,
                                message.RoutingKey,
                                mandatory: false,
                                basicProperties: props,
                                body: body);

                            // Update tracked object directly (no DB call).
                            outboxStore.MarkAsSent(message, DateTime.UtcNow);
                            sentCount++;
                            
                            _logger.LogInformation(
                                "Outbox message published. OutboxId={OutboxId}, MessageId={MessageId}, Provider={Provider}, " +
                                "Exchange={Exchange}, RoutingKey={RoutingKey}, RetryCount={RetryCount}",
                                message.Id,
                                message.MessageId,
                                providerGroup.Key,
                                message.ExchangeName,
                                message.RoutingKey,
                                message.RetryCount);
                        }
                        catch (Exception ex)
                        {
                            // Update tracked object directly (no DB call).
                            outboxStore.MarkAsFailed(message, ex.Message, message.RetryCount + 1);
                            failedCount++;
                            
                            _logger.LogError(
                                ex,
                                "Outbox message publication failed. OutboxId={OutboxId}, MessageId={MessageId}, Provider={Provider}, " +
                                "Exchange={Exchange}, RoutingKey={RoutingKey}, RetryCount={RetryCount}, Reason={Reason}",
                                message.Id,
                                message.MessageId,
                                providerGroup.Key,
                                message.ExchangeName,
                                message.RoutingKey,
                                message.RetryCount,
                                ex.Message);
                        }
                    }
                }
                finally
                {
                    _channelPool.Return(providerGroup.Key, channel);
                }
            }

            // Single batch database save for all tracked changes.
            await outboxStore.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Outbox batch processed. Sent={SentCount}, Failed={FailedCount}, Total={TotalCount}",
                sentCount,
                failedCount,
                sentCount + failedCount);
        }
    }
}
