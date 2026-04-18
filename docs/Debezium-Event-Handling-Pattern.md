# Debezium Event Handling & Pattern Documentation

This document explains the structure of Debezium CDC events, why the data is mapped the way it is in `OrderCdcData`, and the design pattern used to routing and processing these events.

## 1. Debezium Event Structure

Debezium emits complex JSON messages that describe not just the data change, but also the schema of the data at the time of the change. This guarantees that you can strictly type the data even if the database schema changes over time.

### High-Level Structure

An event consists of two main parts:

1.  **`schema`**: Describes the field types (e.g., Int32, String, specific Kafka Connect types like Decimal).
2.  **`payload`**: Contains the actual data.

```json
{
  "schema": { ... },
  "payload": {
    "before": null,           // for INSERTs, this is null. For UPDATES, this is the old row.
    "after": { ... },         // The new state of the row.
    "source": { ... },        // Metadata (Time, Table, LSN, etc.)
    "op": "c",                // Operation: c=create, u=update, d=delete, r=read
    "ts_ms": 1767220163458    // Timestamp
  }
}
```

## 2. Why `OrderCdcData` Defines Fields as Strings

You noticed that `OrderCdcData` defines most fields (like `Id`, `TotalAmount`) as `string`, even if they are GUIDs or Decimals in the database.

### The Reason: Debezium/Kafka Connect Serialization

Debezium uses Kafka Connect's JSON Converter. This converter has specific rules for complex types:

- **UUIDs (`io.debezium.data.Uuid`)**: These are serialized as simple **Strings**.
  - _Code Handling_: We accept `string` and parse it via `Guid.Parse()`.
- **Decimals (`org.apache.kafka.connect.data.Decimal`)**: These are serialized as **Base64 encoded byte arrays**.
  - _Why?_ JSON numbers are floating point and lose precision. Financial data cannot lose precision.
  - _The `OuM=` example_: This is the raw byte representation of the number `500.00` (depending on scale).
  - _Code Handling_: We receive `string` (Base64), decode it to bytes, and interpret it as a `decimal` based on the scale (defined in `schema` as 2).
- **Timestamps (`io.debezium.time.ZonedTimestamp`)**: Serialized as ISO 8601 **Strings**.
  - _Code Handling_: We parse the string to `DateTime`.

### Mapping Table

| PostgreSQL Type | Debezium/Connect Type                   | JSON Representation    | .NET Target Type | Conversion Logic                                  |
| :-------------- | :-------------------------------------- | :--------------------- | :--------------- | :------------------------------------------------ |
| `uuid`          | `io.debezium.data.Uuid`                 | String `"7f8eff..."`   | `Guid`           | `Guid.Parse(value)`                               |
| `decimal(18,2)` | `org.apache.kafka.connect.data.Decimal` | Base64 String `"OuM="` | `decimal`        | Decode Base64 -> BigInteger -> Divide by 10^Scale |
| `timestamp`     | `io.debezium.time.ZonedTimestamp`       | String `"2025-..."`    | `DateTime`       | `DateTime.Parse(value)`                           |
| `int`           | `int32`                                 | Number `0`             | `int`            | Direct mapping                                    |

## 3. The Handling Pattern: Content-Based Router / Strategy Pattern

Your application uses a combination of **Strategy Pattern** and **Content-Based Routing** to handle these events cleanly.

### The Flow

1.  **Ingestion (`CDCConsumerService`)**:
    - Listens to the RabbitMQ queue.
    - Acknowledges the message only if processed successfully.
    - Passes the raw message string to the `CdcMessageHandler`.

2.  **Routing (`CdcMessageHandler`)**:
    - This is the **Router**.
    - It parses the JSON to find two key pieces of information:
      - `source.table`: e.g., "Orders"
      - `op`: e.g., "c" (Create)
    - It asks the **Resolver** for the correct strategy.

3.  **Resolution (`CdcEventHandlerResolver`)**:
    - It maintains a registry (dictionary) mapping `{Table}_{Op}` -> `ICdcEventHandler`.
    - Example: `"Orders_c"` -> returns `OrdersCreateHandler`.

4.  **Execution (`OrdersCreateHandler`)**:
    - This is the **Strategy** implementation.
    - It knows _exactly_ how to handle a CREATE on the ORDERS table.
    - **Steps**:
      1.  Deserialize specific payload to `OrderCdcData`.
      2.  Convert types (String -> Guid/Decimal).
      3.  Execute Business Logic (Save to ReadDB).

### Diagram

```mermaid
classDiagram
    class CdcMessageHandler {
        +HandleMessageAsync()
    }
    class IHandlerResolver {
        +GetHandler(table, op)
    }
    class ICdcEventHandler {
        <<interface>>
        +HandleAsync()
        +TableName
        +Operation
    }
    class OrdersCreateHandler {
        +HandleAsync()
    }
    class OrdersUpdateHandler {
        +HandleAsync()
    }
--docker compose -f DockerCompose.yaml up -d
    CdcMessageHandler --> IHandlerResolver : Resolves
    IHandlerResolver --> ICdcEventHandler : Returns
    OrdersCreateHandler ..|> ICdcEventHandler : Implements
    OrdersUpdateHandler ..|> ICdcEventHandler : Implements
```

### Benefits of this Pattern

1.  **Single Responsibility**: `OrdersCreateHandler` only cares about creating orders. It doesn't care about routing or updates.
2.  **Open/Closed Principle**: To add support for `Payments` table, you create `PaymentsCreateHandler` and register it. You do **not** modify `CdcMessageHandler`.
3.  **Type Safety**: Conversion logic is encapsulated in the Data Model (`OrderCdcData`), keeping the handler clean.
