using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Micro.Shared.Persistence;
using Npgsql;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.EventImplementation.Connections;
using OrderService.Infrastructure.EventImplementation.Inbox;
using OrderService.Infrastructure.EventImplementation.Topology;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Infrastructure.EventImplementation.Consumers;

public sealed class OrderEventsConsumerJob : BackgroundService
{
    private const string QueueName = "order.Q";
    private const string ProviderName = "Broker";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionRegistry _connectionRegistry;
    private readonly IRabbitMqNamePrefixer _namePrefixer;
    private readonly ILogger<OrderEventsConsumerJob> _logger;

    private readonly string _country;
    private readonly SemaphoreSlim _channelGate = new(1, 1);

    private IModel? _channel;

    public OrderEventsConsumerJob(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionRegistry connectionRegistry,
        IRabbitMqNamePrefixer namePrefixer,
        ILogger<OrderEventsConsumerJob> logger,
        string country)
    {
        _scopeFactory = scopeFactory;
        _connectionRegistry = connectionRegistry;
        _namePrefixer = namePrefixer;
        _logger = logger;

        _country = country;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OrderEventsConsumerJob for {Country} is waiting to consume queue {QueueName}.",
            _country,
            GetQueueName());

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnsureConsumer();

                while (!stoppingToken.IsCancellationRequested && _channel is { IsOpen: true })
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderEventsConsumerJob for {Country} stopped unexpectedly.", _country);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                DisposeChannel();
            }
        }
    }

    private void EnsureConsumer()
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        DisposeChannel();

        _channel = _connectionRegistry.GetConnection(ProviderName).CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnReceivedAsync;

        var queueName = GetQueueName();

        _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "OrderEventsConsumerJob for {Country} is consuming queue {QueueName} from provider {Provider}.",
            _country,
                queueName,
            ProviderName);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        await _channelGate.WaitAsync();

        try
        {
            var channel = _channel;
            if (channel is null || !channel.IsOpen)
            {
                _logger.LogWarning(
                    "Skipping delivery because the consumer channel is closed. Country={Country}, DeliveryTag={DeliveryTag}",
                    _country,
                    ea.DeliveryTag);
                return;
            }

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var eventType = ExtractEventType(root);
            var messageId = ExtractMessageId(ea, root);
            var correlationId = ea.BasicProperties?.CorrelationId ?? string.Empty;
            var queueName = GetQueueName();

            _logger.LogInformation(
                "Started processing RabbitMQ message. Country={Country}, MessageId={MessageId}, EventType={EventType}, Queue={Queue}, CorrelationId={CorrelationId}",
                _country,
                messageId,
                eventType,
                queueName,
                correlationId);

            int maxRetry = 3;
            int delaySeconds = 2;
            int currentRetry = 0;
            bool success = false;
            Exception? lastException = null;

            while (currentRetry <= maxRetry)
            {
                using var scope = _scopeFactory.CreateScope();
                var requestContext = scope.ServiceProvider.GetRequiredService<IRequestContext>();
                requestContext.Country = _country;
                requestContext.OperationMode = OperationMode.Write;

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();
                var consumerResolver = scope.ServiceProvider.GetRequiredService<IEventConsumerResolver>();
                var consumer = consumerResolver.Resolve(eventType);

                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, CancellationToken.None);

                try
                {
                    await inboxStore.AddAsync(new InboxMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageId = messageId,
                        EventType = eventType,
                        ProcessedOnUtc = DateTime.UtcNow
                    }, CancellationToken.None);

                    await dbContext.SaveChangesAsync(CancellationToken.None);
                    await consumer.ConsumeAsync(root, CancellationToken.None);
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                    await transaction.CommitAsync(CancellationToken.None);

                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    success = true;

                    _logger.LogInformation(
                        "RabbitMQ message processed and acked. Country={Country}, MessageId={MessageId}, EventType={EventType}, Consumer={Consumer}, Attempt={Attempt}, CorrelationId={CorrelationId}",
                        _country,
                        messageId,
                        eventType,
                        consumer.GetType().Name,
                        currentRetry,
                        correlationId);
                    break;
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    success = true;

                    _logger.LogInformation(
                        ex,
                        "Duplicate inbox message skipped. Country={Country}, MessageId={MessageId}, EventType={EventType}, DeliveryTag={DeliveryTag}, CorrelationId={CorrelationId}",
                        _country,
                        messageId,
                        eventType,
                        ea.DeliveryTag,
                        correlationId);
                    break;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    currentRetry++;
                    lastException = ex;

                    _logger.LogWarning(
                        ex,
                        "Message processing attempt {Attempt} of {MaxRetry} failed. Country={Country}, MessageId={MessageId}, Queue={Queue}, CorrelationId={CorrelationId}",
                        currentRetry,
                        maxRetry,
                        _country,
                        messageId,
                        queueName,
                        correlationId);

                    if (currentRetry <= maxRetry)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                }
            }

            if (!success)
            {
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);

                _logger.LogError(
                    lastException,
                    "Message processing permanently failed after {MaxRetry} attempts. Sent to DLQ. Country={Country}, MessageId={MessageId}, Queue={Queue}, CorrelationId={CorrelationId}",
                    maxRetry,
                    _country,
                    messageId,
                    queueName,
                    correlationId);
            }
        }
        finally
        {
            _channelGate.Release();
        }
    }

    private static Guid ExtractMessageId(BasicDeliverEventArgs ea, JsonElement root)
    {
        if (ea.BasicProperties?.MessageId is not null && Guid.TryParse(ea.BasicProperties.MessageId, out var messageId))
        {
            return messageId;
        }

        if (root.TryGetProperty("Id", out var idProperty) && Guid.TryParse(idProperty.GetString(), out messageId))
        {
            return messageId;
        }

        throw new InvalidOperationException("The message does not contain a valid message id.");
    }

    private static string ExtractEventType(JsonElement root)
    {
        if (root.TryGetProperty("EventType", out var eventTypeProperty))
        {
            var eventType = eventTypeProperty.GetString();
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                return eventType;
            }
        }

        throw new InvalidOperationException("The message does not contain a valid EventType value.");
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                string.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string GetQueueName()
    {
        return _namePrefixer.Prefix(_country, QueueName);
    }

    private void DisposeChannel()
    {
        var channel = _channel;
        _channel = null;

        if (channel is null)
        {
            return;
        }

        try
        {
            if (channel.IsOpen)
            {
                channel.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close RabbitMQ channel cleanly for country {Country}.", _country);
        }
        finally
        {
            channel.Dispose();
        }
    }
}

