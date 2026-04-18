using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.MessagingV2.Topology;

/// <summary>
/// Hosted service that initializes RabbitMQ topology at application startup.
/// Runs before other services and signals completion via TopologyInitializationCoordinator.
/// 
/// If topology initialization fails, this service throws and prevents the application from starting.
/// Dependent services (consumers, dispatchers) will not start until this completes successfully.
/// </summary>
public sealed class RabbitMqTopologyInitializerHostedService : BackgroundService
{
    private readonly RabbitMqTopologyInitializer _initializer;
    private readonly RabbitMqTopologyConfigurator _configurator;
    private readonly TopologyInitializationCoordinator _coordinator;
    private readonly ILogger<RabbitMqTopologyInitializerHostedService> _logger;

    public RabbitMqTopologyInitializerHostedService(
        RabbitMqTopologyInitializer initializer,
        RabbitMqTopologyConfigurator configurator,
        TopologyInitializationCoordinator coordinator,
        ILogger<RabbitMqTopologyInitializerHostedService> logger)
    {
        _initializer = initializer;
        _configurator = configurator;
        _coordinator = coordinator;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ topology initialization service is starting.");

        try
        {
            // Build topology definition
            var topology = _configurator.BuildTopology();

            // Execute topology declarations
            _initializer.InitializeTopology(topology);

            // Signal that initialization is complete; consumers/dispatchers can now start
            _coordinator.Initialize();

            _logger.LogInformation("RabbitMQ topology initialization service completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "RabbitMQ topology initialization failed. Application startup will fail. " +
                "Ensure RabbitMQ broker is running and accessible with configured credentials.");

            // Signal failure to all waiting services
            _coordinator.InitializationFailed(ex);

            // Fail the application startup
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Topology initialization is completed in StartAsync.
        // This method is required by BackgroundService but does minimal work.
        await Task.CompletedTask;
    }
}
