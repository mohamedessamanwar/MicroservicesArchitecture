# RabbitMQ & CDC Flow in OrderService

## Architecture Overview

This document explains the complete flow of Change Data Capture (CDC) using Debezium and RabbitMQ in the OrderService microservice architecture.

![RabbitMQ CDC Flow Diagram](../rabbitmq_cdc_flow.png)

## Components

### 1. PostgreSQL Write Database (OrderDb)

- **Purpose**: Primary database for write operations (CQRS Write Model)
- **Configuration**:
  - WAL (Write-Ahead Log) enabled with `wal_level=logical`
  - Supports logical replication for CDC
  - Connection: `Host=write-db;Port=5432;Database=OrderDb`

### 2. Debezium Server

- **Purpose**: Monitors database changes and publishes them to RabbitMQ
- **Configuration File**: `debezium/orderdb-application.properties`
- **Key Settings**:

  ```properties
  # Source: PostgreSQL
  debezium.source.connector.class=io.debezium.connector.postgresql.PostgresConnector
  debezium.source.database.dbname=OrderDb
  debezium.source.topic.prefix=order-server
  debezium.source.slot.name=order_slot

  # Sink: RabbitMQ
  debezium.sink.type=rabbitmq
  debezium.sink.rabbitmq.exchange=cdc.exchange
  debezium.sink.rabbitmq.routing.key=order.write
  ```

### 3. RabbitMQ Broker

- **Exchange**: `cdc.exchange` (Topic type, durable)
- **Queue**: `order.write.cdc.q` (durable)
- **Binding**: Queue bound to exchange with routing key `order.write`
- **Management UI**: Available at `http://localhost:15672`
- **Credentials**: admin/admin123

### 4. OrderService Application Components

#### 4.1 CDCConsumerService (Background Service)

- **File**: `OrderService.Infrastructure/Messaging/Jobs/CdcConsumerService.cs`
- **Type**: Hosted Background Service
- **Responsibilities**:
  - Maintains connection to RabbitMQ
  - Consumes messages from `order.write.cdc.q`
  - Handles connection failures with retry logic
  - Manual message acknowledgment (autoAck: false)
  - Requeues failed messages

**Key Features**:

```csharp
- AutomaticRecoveryEnabled: true
- NetworkRecoveryInterval: 10 seconds
- RequestedHeartbeat: 60 seconds
- Retry Count: Configurable
- Retry Delay: Configurable
```

#### 4.2 CdcMessageHandler

- **File**: `OrderService.Infrastructure/Messaging/Handlers/CdcHandlers/CdcMessageHandler.cs`
- **Responsibilities**:
  - Parses incoming CDC JSON messages
  - Extracts table name and operation type
  - Routes to appropriate handler via resolver

**Message Structure**:

```json
{
  "source": {
    "table": "orders",
    "db": "OrderDb"
  },
  "op": "c", // c=create, u=update, d=delete
  "after": {
    "id": "guid",
    "customer_id": "guid",
    "total_amount": 100.5,
    "status": 0,
    "created_at": "timestamp",
    "modified": "timestamp"
  }
}
```

#### 4.3 CdcEventHandlerResolver

- **File**: `OrderService.Infrastructure/Messaging/Handlers/CdcHandlers/CdcEventHandlerResolver.cs`
- **Responsibilities**:
  - Resolves the correct handler based on table name and operation
  - Returns null if no handler is registered

#### 4.4 Specific Event Handlers

##### OrdersCreateHandler

- **Operation**: `c` (create)
- **Table**: `orders`
- **Logic**:
  1. Deserializes CDC event
  2. Checks if order already exists (idempotency)
  3. Creates new OrderReadModel
  4. Saves to Read Database

##### OrdersUpdateHandler

- **Operation**: `u` (update)
- **Table**: `orders`
- **Logic**:
  1. Deserializes CDC event
  2. Finds existing order in Read DB
  3. Updates the read model
  4. Saves changes

##### OrdersDeleteHandler

- **Operation**: `d` (delete)
- **Table**: `orders`
- **Logic**:
  1. Deserializes CDC event
  2. Finds order in Read DB
  3. Removes the read model
  4. Saves changes

### 5. PostgreSQL Read Database (OrderReadDb)

- **Purpose**: Optimized for read operations (CQRS Read Model)
- **Contains**: Denormalized OrderReadModel entities
- **Updated By**: CDC event handlers

## Complete Data Flow

### Step-by-Step Process

1. **Write Operation**

   ```
   API Request → OrderService → Write to OrderDb (Write Database)
   ```

2. **Database Change Capture**

   ```
   PostgreSQL WAL Log → Debezium monitors changes via replication slot
   ```

3. **Event Publishing**

   ```
   Debezium → Formats as JSON → Publishes to RabbitMQ
   Exchange: cdc.exchange
   Routing Key: order.write
   ```

4. **Message Routing**

   ```
   RabbitMQ Exchange → Routes based on routing key → Queue: order.write.cdc.q
   ```

5. **Message Consumption**

   ```
   CDCConsumerService → Receives message → Passes to CdcMessageHandler
   ```

6. **Event Processing**

   ```
   CdcMessageHandler → Parses JSON → Extracts table & operation
   → CdcEventHandlerResolver → Returns appropriate handler
   → Handler processes event
   ```

7. **Read Model Sync**

   ```
   Handler → Updates OrderReadDb (Read Database) → CQRS synchronization complete
   ```

8. **Message Acknowledgment**
   ```
   Success: BasicAck → Message removed from queue
   Failure: BasicNack (requeue: true) → Message requeued for retry
   ```

## Configuration

### appsettings.json

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin123",
    "QueueName": "order.write.cdc.q",
    "RoutingKey": "order.write",
    "RetryCount": 5,
    "RetryDelaySeconds": 5
  }
}
```

### Dependency Injection

```csharp
// Configure RabbitMQ settings
services.Configure<RabbitMqConfiguration>(
    configuration.GetSection(RabbitMqConfiguration.SectionName));

// Register CDC Event Handlers (Singleton)
services.AddSingleton<ICdcEventHandler, OrdersCreateHandler>();
services.AddSingleton<ICdcEventHandler, OrdersUpdateHandler>();
services.AddSingleton<ICdcEventHandler, OrdersDeleteHandler>();

// Register Resolver (Singleton)
services.AddSingleton<ICdcEventHandlerResolver, CdcEventHandlerResolver>();

// Register Message Handler (Singleton)
services.AddSingleton<IMessageHandler, CdcMessageHandler>();

// Register Background Service
services.AddHostedService<CDCConsumerService>();
```

## Benefits of This Architecture

### 1. **CQRS Pattern**

- Separate Write and Read models
- Optimized databases for different operations
- Write DB: Normalized, transactional
- Read DB: Denormalized, query-optimized

### 2. **Event-Driven Architecture**

- Asynchronous processing
- Loose coupling between components
- Scalable message processing

### 3. **Reliability**

- Message persistence in RabbitMQ
- Automatic retry on failures
- Idempotent handlers (prevent duplicates)
- Connection recovery

### 4. **Observability**

- Comprehensive logging at each step
- Message tracking with unique IDs
- Connection status monitoring

### 5. **Scalability**

- Multiple consumers can process messages
- RabbitMQ handles load balancing
- Debezium captures all changes reliably

## Error Handling

### Connection Failures

- Automatic reconnection with exponential backoff
- Configurable retry count and delay
- Connection health monitoring

### Message Processing Failures

- Failed messages are requeued
- Logged with full error details
- Manual acknowledgment prevents data loss

### Idempotency

- Handlers check for existing records
- Prevents duplicate processing
- Safe for message redelivery

## Monitoring & Debugging

### RabbitMQ Management UI

- View queues, exchanges, bindings
- Monitor message rates
- Check consumer connections
- URL: `http://localhost:15672`

### Logs to Monitor

```
- "RabbitMQ Consumer Service is starting..."
- "Successfully connected to RabbitMQ. Listening on queue: {QueueName}"
- "Received message {MessageId} from queue: {QueueName}"
- "CDC Event - Table: {Table}, Operation: {Operation}"
- "Successfully processed and acknowledged message {MessageId}"
```

### Common Issues

1. **Connection Refused**

   - Check if RabbitMQ is running
   - Verify host and port configuration
   - Check credentials

2. **Messages Not Being Consumed**

   - Verify queue binding to exchange
   - Check routing key matches
   - Ensure consumer is connected

3. **Debezium Not Publishing**
   - Check PostgreSQL WAL configuration
   - Verify replication slot exists
   - Check Debezium logs

## Docker Compose Setup

The complete infrastructure is defined in `DockerCompose.yaml`:

```yaml
services:
  write-db: # PostgreSQL with WAL enabled
  rabbitmq: # RabbitMQ broker
  rabbitmq-init: # Creates exchange/queues/bindings
  db-init: # Creates databases
  debezium-order: # Debezium for OrderDb
  order-service: # OrderService application
```

### Startup Order

1. write-db (PostgreSQL)
2. rabbitmq
3. rabbitmq-init (creates infrastructure)
4. db-init (creates databases)
5. debezium-order (starts monitoring)
6. order-service (starts consuming)

## Testing the Flow

### 1. Create an Order

```bash
POST http://localhost:8080/api/orders
{
  "customerId": "guid",
  "totalAmount": 100.50
}
```

### 2. Check Write Database

```sql
SELECT * FROM orders;
```

### 3. Check RabbitMQ

- Open management UI
- View messages in `order.write.cdc.q`

### 4. Check Logs

- OrderService logs should show message processing
- Debezium logs should show event capture

### 5. Check Read Database

```sql
SELECT * FROM order_read_models;
```

## Conclusion

This architecture implements a robust, scalable, and maintainable CDC pipeline using Debezium and RabbitMQ. It enables real-time synchronization between Write and Read databases while maintaining loose coupling and high reliability.
