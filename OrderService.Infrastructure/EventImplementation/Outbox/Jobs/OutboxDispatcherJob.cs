using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Micro.Shared.Persistence;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.EventImplementation.Connections;
using OrderService.Infrastructure.EventImplementation.Topology;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.EventImplementation.Outbox;

public sealed class OutboxDispatcherJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChannelPool _channelPool;
    private readonly IRabbitMqNamePrefixer _namePrefixer;
    private readonly ILogger<OutboxDispatcherJob> _logger;
    private readonly string _country;

    public OutboxDispatcherJob(
        IServiceScopeFactory scopeFactory,
        IChannelPool channelPool,
        IRabbitMqNamePrefixer namePrefixer,
        ILogger<OutboxDispatcherJob> logger,
        string country)
    {
        _scopeFactory = scopeFactory;
        _channelPool = channelPool;
        _namePrefixer = namePrefixer;
        _logger = logger;
        _country = country;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OutboxDispatcherJob for {Country} is waiting for topology initialization to complete.", _country);

        _logger.LogInformation("OutboxDispatcherJob for {Country} initialized and ready to dispatch messages.", _country);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var requestContext = scope.ServiceProvider.GetRequiredService<IRequestContext>();
                requestContext.Country = _country;
                requestContext.OperationMode = OperationMode.Write;

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
                                _logger.LogInformation(
                                    "Processing outbox message. OutboxId={OutboxId}, MessageId={MessageId}, Provider={Provider}",
                                    message.Id,
                                    message.MessageId,
                                    providerGroup.Key);

                                var exchangeName = _namePrefixer.Prefix(_country, message.ExchangeName);
                                var routingKey = _namePrefixer.Prefix(_country, message.RoutingKey);

                                // Publish message; assume exchange is pre-provisioned.
                                var body = Encoding.UTF8.GetBytes(message.Payload);
                                var props = channel.CreateBasicProperties();
                                props.MessageId = message.MessageId.ToString();
                                props.Persistent = true;
                                
                                channel.BasicPublish(
                                    exchangeName,
                                    routingKey,
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
                                    exchangeName,
                                    routingKey,
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
                    "Outbox batch processed for {Country}. Sent={SentCount}, Failed={FailedCount}, Total={TotalCount}",
                    _country,
                    sentCount,
                    failedCount,
                    sentCount + failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "An error occurred while processing the outbox batch for {Country}. Messages might be retried on the next run.", 
                    _country);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

