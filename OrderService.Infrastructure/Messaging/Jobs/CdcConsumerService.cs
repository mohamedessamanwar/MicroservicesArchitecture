//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using OrderService.Infrastructure.Messaging.Handlers;
//using OrderService.Infrastructure.Messaging.RabbitMQ;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using System.Text;
//using RabbitMqConfig = OrderService.Infrastructure.Messaging.RabbitMqConfiguration.RabbitMqConfiguration;

//namespace OrderService.Infrastructure.Messaging.Jobs;

//public class CDCConsumerService : BackgroundService
//{
//    private readonly ILogger<CDCConsumerService> _logger;
//    private readonly RabbitMqConfig _config;
//    private readonly IMessageHandler _messageHandler;
//    private readonly IRabbitMQConnectionManager _connectionManager;
//    private readonly RabbitMQTopologyInitializer _topology;
//    private IModel? _channel;

//    public CDCConsumerService(
//        ILogger<CDCConsumerService> logger,
//        IOptions<RabbitMqConfig> config,
//        IMessageHandler messageHandler,
//        IRabbitMQConnectionManager connectionManager,
//        RabbitMQTopologyInitializer topology)
//    {
//        _logger = logger;
//        _config = config.Value;
//        _messageHandler = messageHandler;
//        _connectionManager = connectionManager;
//        _topology = topology;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("CDC RabbitMQ Consumer Service is starting...");
//        stoppingToken.Register(() => _logger.LogInformation("CDC RabbitMQ Consumer Service is stopping..."));

//        var retryCount = 0;
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                if (_channel == null || !_channel.IsOpen)
//                {
//                    _channel?.Close();
//                    _channel?.Dispose();
//                    _topology.DeclareAll(); // idempotent; ensure topology before consume
//                    _channel = _connectionManager.CreateChannel();
//                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

//                    var consumer = new EventingBasicConsumer(_channel);
//                    consumer.Received += async (_, ea) => await OnMessageReceivedAsync(ea, stoppingToken);

//                    _channel.BasicConsume(
//                        queue: _config.QueueName,
//                        autoAck: false,
//                        consumer: consumer);

//                    _logger.LogInformation(
//                        "CDC Consumer connected. Listening on queue: {QueueName}, routing key: {RoutingKey}",
//                        _config.QueueName, _config.RoutingKey);
//                    retryCount = 0;
//                }

//                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("CDC RabbitMQ Consumer Service is being cancelled");
//                break;
//            }
//            catch (Exception ex)
//            {
//                retryCount++;
//                _logger.LogError(ex, "Error in CDC Consumer main loop (retry {Retry})", retryCount);
//                await Task.Delay(TimeSpan.FromSeconds(_config.RetryDelaySeconds), stoppingToken);
//            }
//        }

//        _logger.LogInformation("CDC RabbitMQ Consumer Service has stopped");
//    }

//    private async Task OnMessageReceivedAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
//    {
//        var channel = _channel;
//        if (channel == null || !channel.IsOpen) return;

//        try
//        {
//            var body = ea.Body.ToArray();
//            var message = Encoding.UTF8.GetString(body);
//            await _messageHandler.HandleMessageAsync(message, ea.RoutingKey, cancellationToken);
//            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error processing CDC message. Nacking (requeue: false) -> DLQ. DeliveryTag: {Tag}", ea.DeliveryTag);
//            try
//            {
//                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
//            }
//            catch (Exception nackEx)
//            {
//                _logger.LogError(nackEx, "BasicNack failed for DeliveryTag: {Tag}", ea.DeliveryTag);
//            }
//        }
//    }

//    public override void Dispose()
//    {
//        _logger.LogInformation("Disposing CDC RabbitMQ Consumer Service...");
//        try
//        {
//            _channel?.Close();
//            _channel?.Dispose();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error disposing CDC consumer channel");
//        }
//        base.Dispose();
//    }
//}
