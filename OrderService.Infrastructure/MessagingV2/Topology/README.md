# RabbitMQ Topology Initializer Implementation Guide

## Overview

This implementation provides a **production-ready RabbitMQ topology initialization mechanism** that executes at application startup, completing **before** any consumer or publisher background services begin processing. It ensures durable, fault-tolerant message infrastructure with dead-letter queue support.

## Design Architecture

### Core Components

1. **RabbitMqTopologyDefinition**
   - Immutable data structure holding all exchanges, queues, and bindings
   - Includes validation to ensure topology consistency
   - Discovered from actual application usage patterns

2. **RabbitMqTopologyConfigurator**
   - Builds the topology definition from event routing registry
   - Centralizes topology discovery logic
   - Returns validated topology ready for declaration

3. **RabbitMqTopologyInitializer**
   - Executes actual RabbitMQ declarations (ExchangeDeclare, QueueDeclare, QueueBind)
   - Comprehensive logging of each declaration step
   - Idempotent: safe to run repeatedly with consistent definitions

4. **RabbitMqTopologyInitializerHostedService**
   - BackgroundService that runs during application startup
   - Executes topology initialization in StartAsync()
   - Signals completion via TopologyInitializationCoordinator
   - Fails application startup if topology initialization fails

5. **TopologyInitializationCoordinator**
   - Startup gate using TaskCompletionSource
   - Ensures ordered initialization: topology ? consumers/dispatcher
   - Consumers and dispatcher wait via WaitForInitializationAsync()

### Startup Ordering

```
Application Start
    ?
Dependency Injection Setup
    ?
RabbitMqTopologyInitializerHostedService.StartAsync()
    ?? Build topology definition
    ?? Declare all exchanges
    ?? Declare all queues
    ?? Create all bindings
    ?? Signal TopologyInitializationCoordinator.Initialize()
    ?
OutboxDispatcherJob.StartAsync()
    ?? Wait for TopologyInitializationCoordinator
        ?
        [Ready to publish]
    
OrderCreatedConsumerJob.StartAsync()
    ?? Wait for TopologyInitializationCoordinator
        ?
        [Ready to consume]
```

## Discovered Topology

The following topology was discovered from the existing codebase:

### Exchanges
- **billing.exchange** (direct type, durable)
  - Purpose: Main exchange for order-related events
  - Source: EventRoutingRegistry for OrderCreatedEvent

### Queues
- **order.created.q** (durable, with dead-lettering enabled)
  - Purpose: Receives OrderCreatedEvent messages
  - Consumed by: OrderCreatedConsumerJob
  - Dead-letter configuration: Routes failed messages to order.created.q.dlx

- **order.created.q.dlq** (durable)
  - Purpose: Captures messages that failed processing or expired
  - Enables monitoring and manual intervention for failed messages

### Dead-Letter Exchanges
- **order.created.q.dlx** (direct type, durable)
  - Purpose: Routes failed/expired messages from order.created.q
  - Bound to: order.created.q.dlq

### Bindings
- **billing.exchange ? order.created.q** with routing key **order.created**
  - Purpose: Routes published OrderCreatedEvent to consumer queue
  
- **order.created.q.dlx ? order.created.q.dlq** with routing key **order.created**
  - Purpose: Routes dead-lettered messages to isolation queue

## Naming Convention

The implementation uses a consistent, predictable dead-letter naming convention:

```
Main Queue:           {queue-name}
Dead-Letter Exchange: {queue-name}.dlx
Dead-Letter Queue:    {queue-name}.dlq
```

Example: `order.created.q` ? `order.created.q.dlx` / `order.created.q.dlq`

This convention is documented in `QueueDefinition.DeadLetterExchangeName` and `QueueDefinition.DeadLetterQueueName` properties.

## Durability & Failure Recovery

### Durable Topology
- All exchanges declared with `durable: true`
- All queues declared with `durable: true`
- Topology survives RabbitMQ broker restarts

### Persistent Messages
- OutboxDispatcherJob sets `props.Persistent = true` before publishing
- Messages survive RabbitMQ broker restarts while in queues

### Dead-Letter Handling
- When OrderCreatedConsumerJob nacks without requeue (processing failure)
- When messages exceed TTL (if configured)
- When queue exceeds max length (if configured)

**Result**: Failed messages are automatically moved to dead-letter queues for monitoring and recovery.

## Integration with Existing Services

### OrderCreatedConsumerJob Changes
**Before**: Assumed topology was pre-provisioned externally  
**After**: Explicitly waits for TopologyInitializationCoordinator

**Key changes**:
- Constructor now requires `TopologyInitializationCoordinator`
- StartAsync() calls `WaitForInitializationAsync()` before creating channel
- Uses hardcoded queue name `"order.created.q"` instead of OrderCreatedConsumerTopology constant
- Updated logging to clarify dependency on topology initialization

### OutboxDispatcherJob Changes
**Before**: Assumed exchanges were pre-provisioned  
**After**: Explicitly waits for TopologyInitializationCoordinator

**Key changes**:
- Constructor now requires `TopologyInitializationCoordinator`
- Overrides StartAsync() to wait for initialization before proceeding
- Updated logging to clarify dependency on topology initialization

### Removed: OrderCreatedConsumerTopology
This file defined topology constants (`Queue`, `Exchange`, `RoutingKey`, `ProviderName`).  
**Replaced by**: Centralized topology definition in RabbitMqTopologyConfigurator.

## Dependency Injection Registration

Update your `Program.cs` or registration file (already updated in `MessagingV2Registration.cs`):

```csharp
services.AddMessagingV2(configuration);
services.AddOrderMessagingConsumerJobs();
```

The `AddMessagingV2()` extension now registers:
- TopologyInitializationCoordinator (singleton)
- RabbitMqTopologyConfigurator (singleton)
- RabbitMqTopologyInitializer (singleton)
- RabbitMqTopologyInitializerHostedService (hosted service)

These are registered **before** OutboxDispatcherJob and consumer jobs, ensuring proper startup ordering.

## Idempotency & Recoverability

### RabbitMQ Declaration Idempotency
RabbitMQ allows declaring the same exchange/queue/binding multiple times if:
- Exchange type matches
- Queue parameters match (durable, exclusive, autoDelete)
- Binding parameters match

**Risk**: If topology definition changes (e.g., queue becomes non-durable), declaration will fail.  
**Mitigation**: Ensure topology definitions remain consistent; document changes before rolling out.

### Initialization Failure Behavior
If topology initialization fails:
1. RabbitMqTopologyInitializerHostedService throws exception
2. TopologyInitializationCoordinator.InitializationFailed() is called
3. All waiting services receive the exception
4. Application startup fails (no silent fallback)
5. Clear error logging shows cause and remediation steps

## Logging

The implementation includes comprehensive structured logging:

### RabbitMqTopologyInitializerHostedService
- Application startup/completion messages
- Failure notifications with remediation guidance

### RabbitMqTopologyInitializer
- Exchange declaration (name, type, durability)
- Queue declaration (name, durability, dead-letter config if present)
- Binding creation (exchange, queue, routing key)
- Detailed error logging with exchange/queue/routing key context

### OrderCreatedConsumerJob
- Waiting for topology initialization
- Ready to consume notification
- Failure indication with broker accessibility check

### OutboxDispatcherJob
- Waiting for topology initialization
- Ready to dispatch notification
- Failure indication with broker accessibility check

## Configuration

No additional configuration is required beyond existing `Messaging` section in appsettings.json.

Existing configuration structure is used:
```json
{
  "Messaging": {
    "Providers": [
      {
        "Name": "BillingBroker",
        "Host": "localhost",
        "Port": 5672,
        "VirtualHost": "/",
        "Username": "guest",
        "Password": "guest",
        "HeartbeatSeconds": 30
      }
    ]
  }
}
```

## Testing Recommendations

### Unit Tests
1. **RabbitMqTopologyConfigurator**: Verify topology structure (exchanges, queues, bindings)
2. **RabbitMqTopologyDefinition.Validate()**: Test consistency validation
3. **TopologyInitializationCoordinator**: Test gate blocking/signaling behavior

### Integration Tests
1. **RabbitMqTopologyInitializer**: Declare topology against test RabbitMQ broker
2. **Startup ordering**: Verify consumers/dispatcher wait for initialization
3. **Idempotency**: Run topology initialization twice, verify no errors

### Manual Testing
1. Start application, verify logs show topology declaration success
2. Publish OrderCreatedEvent, verify consumed by OrderCreatedConsumerJob
3. Simulate processing failure (throw in consumer), verify message dead-lettered
4. Verify dead-lettered message in `order.created.q.dlq`

## Production Considerations

### Monitoring
- Set alerts on RabbitMQ broker state (reachability, memory, disk)
- Monitor application logs for topology initialization failures
- Monitor dead-letter queue depth (indicates processing failures)

### Scaling
- Topology initialization is singleton; performed once per application instance
- Multiple application instances can run concurrently (topology declarations are idempotent)
- Dead-letter queue depth may grow if processing failures are common

### Disaster Recovery
- If RabbitMQ broker is lost, topology is automatically re-created when broker comes back online
- Persistent messages in queues are recovered
- Failed messages in dead-letter queues are available for recovery/replay

### Configuration Changes
- To add a new exchange/queue/binding: Update RabbitMqTopologyConfigurator.Build*() methods
- To add a new consumer: Register in DI and follow OrderCreatedConsumerJob pattern
- To add a new event: Register in EventRoutingRegistry, then update topology configurator

## Files Added

```
OrderService.Infrastructure/
??? MessagingV2/
    ??? Topology/
        ??? Definitions/
        ?   ??? ExchangeDefinition.cs
        ?   ??? QueueDefinition.cs
        ?   ??? BindingDefinition.cs
        ??? RabbitMqTopologyDefinition.cs
        ??? RabbitMqTopologyConfigurator.cs
        ??? RabbitMqTopologyInitializer.cs
        ??? RabbitMqTopologyInitializerHostedService.cs
        ??? TopologyInitializationCoordinator.cs
```

## Files Modified

```
OrderService.Infrastructure/
??? MessagingV2/
?   ??? ConsumerServices/
?   ?   ??? OrderCreatedConsumerJob.cs (updated to wait for topology)
?   ??? Outbox/
?       ??? OutboxDispatcherJob.cs (updated to wait for topology)
??? Dependency/
    ??? MessagingV2Registration.cs (registers topology initializer services)
```

## Files Removed

```
OrderService.Infrastructure/
??? MessagingV2/
    ??? ConsumerServices/
        ??? OrderCreatedConsumerTopology.cs (removed - centralized in RabbitMqTopologyConfigurator)
```

## Key Assumptions

1. **Single RabbitMQ Provider**: Currently configured for "BillingBroker"; extensible for multiple providers
2. **OrderCreatedEvent Routing**: Topology based on discovered EventRoutingRegistry entry for OrderCreatedEvent
3. **Dead-Lettering Strategy**: Failed messages (nack without requeue) go to dead-letter queues
4. **Queue Naming**: Follows pattern `{event-name}.q`; dead-letters follow `.dlx`/`.dlq` convention
5. **Broker Accessibility**: Application startup fails if RabbitMQ broker is unreachable
6. **No Message TTL**: Dead-lettering configured for processing failures; TTL not enabled by default

## Future Extensions

### Adding a New Queue/Consumer
1. Add new event route in EventRoutingRegistry
2. Create queue/binding definition in RabbitMqTopologyConfigurator
3. Implement consumer class (IConsumer<T>)
4. Register in DI
5. Consumer automatically waits for topology via coordinator

### Dead-Letter Recovery
Implement a background job that:
1. Monitors dead-letter queues
2. Republishes messages after remediation
3. Tracks requeue attempts

### Message TTL
Enable in QueueDefinition if aging-out messages is desired:
```csharp
queue.SetMessageTtlMilliseconds(86400000); // 24 hours
```

### Per-Provider Topology
Currently initialized for single "BillingBroker" provider. To support multiple:
1. Extend RabbitMqTopologyConfigurator to build per-provider topologies
2. Loop through MessagingOptions.Providers in initializer
3. Initialize each provider's topology independently
