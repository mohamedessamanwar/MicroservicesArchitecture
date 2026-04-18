# Messaging Infrastructure V2 Documentation

This document outlines the new messaging infrastructure implemented in the `OrderService`.

## Overview

The new infrastructure (V2) uses the **Outbox Pattern** for reliable event publishing and a reflection-based **Consumer** for dynamic event handling.

## Components

### 1. Outbox Pattern (Publisher)

- **`EventPublisher`**: Enqueues events into the `OutboxMessages` table in the database within the same transaction as the business operation (if valid).
- **`OutboxMessage` Entity**: specifices the schema for stored events.
- **`RabbitMQDispatcher`**: A background service that polls `OutboxMessages`, publishes them to RabbitMQ, and marks them as published.

### 2. RabbitMQ Consumer

- **`RabbitMQConsumer`**: A background service that listens to the queue `q1`.
- **Reflection Dispatch**: It dynamically resolves the Event Type (from `OrderService.Domain.Events`) and the corresponding Consumer (from `OrderService.Infrastructure.MessagingV2`) based on the message payload.
- **`IConsumer<T>`**: Standard interface for all consumers.

### 3. Example Flow (Add Product)

1.  **Trigger**: API calls `TestEventController` / or Business Logic calls `IEventPublisher.EnqueueAsync`.
2.  **Storage**: Event is saved to `OutboxMessages`.
3.  **Dispatch**: `RabbitMQDispatcher` picks it up and sends to Exchange `ex1` with key `key1`.
4.  **Routing**: RabbitMQ routes it to queue `q1`.
5.  **Consumption**: `RabbitMQConsumer` reads from `q1`.
    - Deserializes `AddProductEvent`.
    - Instantiates `AddProductConsumer`.
    - Calls `ConsumeAsync`.
6.  **Processing**: `AddProductConsumer` creates a MediatR command `AddProductDescriptionChunkCommand` and sends it.

## Configuration

The current setup uses hardcoded RabbitMQ configuration in the implementation (Host: `localhost`, Port: `5672`).

- **Exchange**: `ex1` (Direct)
- **Queue**: `q1`
- **Routing Key**: `key1`

## Dependencies

- `RabbitMQ.Client` (Version 6.x used for compatibility)
- `MediatR`
- `Microsoft.EntityFrameworkCore`

## How to Extend

1.  Create a new Event in `OrderService.Domain.Events`.
2.  Create a new Consumer in `OrderService.Infrastructure.MessagingV2` implementing `IConsumer<NewEvent>`.
    - _Naming Convention_: `NewEvent` -> `NewConsumer`.
3.  Ensure the Consumer Logic is implemented in `ConsumeAsync`.
