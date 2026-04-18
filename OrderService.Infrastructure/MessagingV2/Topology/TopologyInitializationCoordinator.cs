using System.Threading;

namespace OrderService.Infrastructure.MessagingV2.Topology;

/// <summary>
/// Startup gate ensuring RabbitMQ topology initialization completes before dependent background services begin.
/// 
/// Pattern:
/// 1. TopologyInitializerHostedService calls Initialize() once at startup
/// 2. Consumer and dispatcher hosted services call WaitForInitializationAsync() before beginning work
/// 3. All dependent services block until Initialize() is called, ensuring topology exists before any processing
/// 
/// This ensures ordered startup: topology ? consumers/dispatchers ? application processing.
/// </summary>
public sealed class TopologyInitializationCoordinator
{
    private readonly TaskCompletionSource<bool> _initializationGate = new();

    /// <summary>
    /// Signals that RabbitMQ topology initialization has completed successfully.
    /// Unblocks all callers of WaitForInitializationAsync().
    /// </summary>
    public void Initialize()
    {
        _initializationGate.TrySetResult(true);
    }

    /// <summary>
    /// Blocks until topology initialization is complete.
    /// Safe to call multiple times; subsequent calls return immediately after first initialization.
    /// </summary>
    public Task WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
        return _initializationGate.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Signals initialization failure.
    /// Causes all WaitForInitializationAsync() calls to throw the provided exception.
    /// </summary>
    public void InitializationFailed(Exception exception)
    {
        _initializationGate.TrySetException(exception);
    }
}
