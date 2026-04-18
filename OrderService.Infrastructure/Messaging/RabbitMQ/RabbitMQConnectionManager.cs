//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using RabbitMQ.Client;
//using RabbitMqConfig = OrderService.Infrastructure.Messaging.RabbitMqConfiguration.RabbitMqConfiguration;

//namespace OrderService.Infrastructure.Messaging.RabbitMQ;

//public sealed class RabbitMQConnectionManager : IRabbitMQConnectionManager
//{
//    private readonly ILogger<RabbitMQConnectionManager> _logger;
//    private readonly RabbitMqConfig _config;
//    private IConnection? _connection;
//    private readonly object _lock = new();
//    private bool _disposed;

//    public RabbitMQConnectionManager(
//        ILogger<RabbitMQConnectionManager> logger,
//        IOptions<RabbitMqConfig> config)
//    {
//        _logger = logger;
//        _config = config.Value;
//    }

//    public bool IsConnected
//    {
//        get { lock (_lock) return _connection?.IsOpen == true; }
//    }

//    public IConnection Connection
//    {
//        get
//        {
//            EnsureConnection();
//            return _connection!;
//        }
//    }

//    public IModel CreateChannel()
//    {
//        return Connection.CreateModel();
//    }

//    private void EnsureConnection()
//    {
//        lock (_lock)
//        {
//            if (_connection?.IsOpen == true) return;
//            if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQConnectionManager));

//            var factory = new ConnectionFactory
//            {
//                HostName = _config.Host,
//                Port = _config.Port,
//                UserName = _config.Username,
//                Password = _config.Password,
//                AutomaticRecoveryEnabled = true,
//                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
//                RequestedHeartbeat = TimeSpan.FromSeconds(60),
//                DispatchConsumersAsync = true
//            };

//            _connection = factory.CreateConnection();
//            _connection.ConnectionShutdown += (_, e) =>
//                _logger.LogWarning("RabbitMQ connection shutdown. Reason: {Reason}", e.ReplyText);
//            _connection.CallbackException += (_, e) =>
//                _logger.LogError(e.Exception, "RabbitMQ callback exception");
//            _connection.ConnectionBlocked += (_, e) =>
//                _logger.LogWarning("RabbitMQ connection blocked. Reason: {Reason}", e.Reason);

//            _logger.LogInformation("RabbitMQ connection established (singleton).");
//        }
//    }

//    public void Dispose()
//    {
//        lock (_lock)
//        {
//            if (_disposed) return;
//            try
//            {
//                _connection?.Close(TimeSpan.FromSeconds(5));
//                _connection?.Dispose();
//                _logger.LogInformation("RabbitMQ connection closed.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error closing RabbitMQ connection.");
//            }
//            _disposed = true;
//        }
//    }
//}
