//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using OrderService.Infrastructure.MessagingV2;
//using RabbitMQ.Client;
//using RabbitMqConfig = OrderService.Infrastructure.Messaging.RabbitMqConfiguration.RabbitMqConfiguration;

//namespace OrderService.Infrastructure.Messaging.RabbitMQ;

///// <summary>
///// Declares exchanges, queues, and bindings idempotently at startup.
///// Uses a single channel then disposes it. Run once (e.g. in a hosted service or at app start).
///// </summary>
//public sealed class RabbitMQTopologyInitializer
//{
//    private readonly ILogger<RabbitMQTopologyInitializer> _logger;
//    private readonly IRabbitMQConnectionManager _connectionManager;
//    private readonly RabbitMqConfig _rabbitConfig;
//    private readonly MessagingV2Options _v2Options;

//    public RabbitMQTopologyInitializer(
//        ILogger<RabbitMQTopologyInitializer> logger,
//        IRabbitMQConnectionManager connectionManager,
//        IOptions<RabbitMqConfig> rabbitConfig,
//        IOptions<MessagingV2Options> v2Options)
//    {
//        _logger = logger;
//        _connectionManager = connectionManager;
//        _rabbitConfig = rabbitConfig.Value;
//        _v2Options = v2Options.Value;
//    }

//    public void DeclareAll()
//    {
//        using var channel = _connectionManager.CreateChannel();
//        DeclareCdcTopology(channel);
//        if (!string.IsNullOrEmpty(_v2Options.ExchangeName))
//            DeclareMessagingV2Topology(channel);
//    }

//    private void DeclareCdcTopology(IModel channel)
//    {
//        if (string.IsNullOrEmpty(_rabbitConfig.QueueName) || string.IsNullOrEmpty(_rabbitConfig.Exchange)) return;

//        var exchange = _rabbitConfig.Exchange;
//        var queue = _rabbitConfig.QueueName;
//        var routingKey = _rabbitConfig.RoutingKey;
//        var dlx = _rabbitConfig.DeadLetterExchange;
//        var dlq = _rabbitConfig.DeadLetterQueue;
//        var dlRoutingKey = _rabbitConfig.DeadLetterRoutingKey;
//        var ttl = _rabbitConfig.MessageTtlMs;
//        var maxLen = _rabbitConfig.MaxLength;

//        if (!string.IsNullOrEmpty(dlx))
//        {
//            channel.ExchangeDeclare(dlx, ExchangeType.Direct, durable: true, autoDelete: false);
//            channel.QueueDeclare(dlq, durable: true, exclusive: false, autoDelete: false, arguments: null);
//            channel.QueueBind(dlq, dlx, string.IsNullOrEmpty(dlRoutingKey) ? "dlq" : dlRoutingKey);
//        }
        
//        channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);

//        var queueArgs = new Dictionary<string, object>();
//        if (ttl > 0) queueArgs["x-message-ttl"] = ttl;
//        if (maxLen > 0) queueArgs["x-max-length"] = maxLen;
//        if (!string.IsNullOrEmpty(dlx))
//        {
//            queueArgs["x-dead-letter-exchange"] = dlx;
//            queueArgs["x-dead-letter-routing-key"] = string.IsNullOrEmpty(dlRoutingKey) ? "dlq" : dlRoutingKey;
//        }

//        channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false,
//            arguments: queueArgs.Count > 0 ? queueArgs : null);
//        channel.QueueBind(queue, exchange, routingKey);

//        _logger.LogInformation("CDC topology declared: exchange={Exchange}, queue={Queue}, dlx={Dlx}, dlq={Dlq}",
//            exchange, queue, dlx, dlq);
//    }

//    private void DeclareMessagingV2Topology(IModel channel)
//    {

//        var exchange = _v2Options.ExchangeName;
//        var queue = _v2Options.QueueName;
//        var routingKey = _v2Options.RoutingKey;
//        var dlx = _v2Options.DeadLetterExchange;
//        var dlq = _v2Options.DeadLetterQueue;
//        var dlRoutingKey = _v2Options.DeadLetterRoutingKey;
//        var ttl = _v2Options.MessageTtlMs;
//        var maxLen = _v2Options.MaxLength;

//        channel.ExchangeDeclare(dlx, ExchangeType.Direct, durable: true, autoDelete: false);
//        channel.QueueDeclare(dlq, durable: true, exclusive: false, autoDelete: false, arguments: null);
//        channel.QueueBind(dlq, dlx, dlRoutingKey);

//        channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);

//        var queueArgs = new Dictionary<string, object>();
//        if (ttl > 0) queueArgs["x-message-ttl"] = ttl;
//        if (maxLen > 0) queueArgs["x-max-length"] = maxLen;
//        queueArgs["x-dead-letter-exchange"] = dlx;
//        queueArgs["x-dead-letter-routing-key"] = dlRoutingKey;

//        channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
//        channel.QueueBind(queue, exchange, routingKey);

//        _logger.LogInformation("MessagingV2 topology declared: exchange={Exchange}, queue={Queue}, dlx={Dlx}, dlq={Dlq}",
//            exchange, queue, dlx, dlq);
//    }
//}
