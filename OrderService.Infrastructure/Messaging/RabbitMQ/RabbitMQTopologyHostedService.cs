//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using OrderService.Infrastructure.Messaging.RabbitMQ;

//namespace OrderService.Infrastructure.Messaging.RabbitMQ;

///// <summary>
///// Ensures topology is declared once at startup (after connection is available).
///// </summary>
//public sealed class RabbitMQTopologyHostedService : IHostedService
//{
//    private readonly ILogger<RabbitMQTopologyHostedService> _logger;
//    private readonly IRabbitMQConnectionManager _connectionManager;
//    private readonly RabbitMQTopologyInitializer _topology;

//    public RabbitMQTopologyHostedService(
//        ILogger<RabbitMQTopologyHostedService> logger,
//        IRabbitMQConnectionManager connectionManager,
//        RabbitMQTopologyInitializer topology)
//    {
//        _logger = logger;
//        _connectionManager = connectionManager;
//        _topology = topology;
//    }

//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        try
//        {
//            _ = _connectionManager.Connection; // ensure connection exists
//            _topology.DeclareAll();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to declare RabbitMQ topology at startup.");
//        }

//        return Task.CompletedTask;
//    }

//    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
//}
