---
name: microservices-architecture
description: Details the specific business logic of each microservice and the components of the overall system architecture.
---

# Microservices Architecture & Components

This skill details the components of the system and the domain logic for each individual microservice.

## Infrastructure Components
1. **API Gateway (YARP):** The centralized entry point for all client requests. Handles routing, rate limiting, and forwards the `X-Country` header for multi-tenancy.
2. **PostgreSQL (Primary & Replica):** Each service has its own logical database within Postgres. We use Logical Replication to sync data to a Read-Replica. PgBouncer is used for connection pooling.
3. **Redis:** Used for distributed caching and token-bucket rate limiting.
4. **RabbitMQ:** The message broker handling asynchronous events between services using Topic exchanges.

## Service Specific Business Logic

### 1. Order Service
- **Domain:** Manages the lifecycle of an order (Pending, Paid, Shipped, Cancelled).
- **Architecture Pattern:** CQRS (Command Query Responsibility Segregation).
- **Resilience:** Uses Saga Pattern for distributed transactions. Outbox Pattern is used to guarantee message delivery to RabbitMQ.

### 2. Payment Service
- **Domain:** Handles processing charges and refunds for user orders.
- **Architecture Pattern:** Event-Driven Consumer. It listens to `OrderCreated` events and attempts to charge the user.
- **Resilience:** Uses Inbox Pattern for idempotent processing (avoids double-charging users).

### 3. Product Service
- **Domain:** Manages the product catalog, pricing, and inventory stock levels.
- **Architecture Pattern:** CRUD & Event-Driven.
- **Resilience:** Needs to handle concurrent inventory deductions safely (optimistic concurrency) and rollback inventory if a Saga compensation event is received.
