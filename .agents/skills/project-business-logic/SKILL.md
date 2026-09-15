---
name: project-business-logic
description: Contains the high-level business logic and core goals of the Microservices Architecture E-Commerce platform.
---

# Project Business Logic

This skill provides the overarching business logic for the entire E-Commerce platform.

## Core Platform Goals
1. **E-Commerce Order Fulfillment:** The system handles the complete lifecycle of a customer order, from placement to payment and inventory deduction.
2. **Multi-Tenancy (Geographical Isolation):** The platform operates in multiple regions (e.g., Egypt, USA). Data is strictly isolated per country using separate database schemas/instances to comply with local regulations and improve performance.
3. **High Availability & Resilience:** The system is designed as a distributed microservices architecture to ensure that failures in one domain (e.g., Payment) do not cascade and bring down the entire system.
4. **Event-Driven Architecture:** Asynchronous communication is preferred for cross-domain operations using RabbitMQ to ensure loose coupling.

## Key Business Flows
- **Checkout Process:** When a user checks out, an order is placed in a "Pending" state.
- **Inventory Reservation:** The Product service must reserve or deduct the items.
- **Payment Processing:** The Payment service must process the user's payment.
- **Saga Orchestration/Choreography:** If any step in the checkout process fails (e.g., payment declined), a compensation transaction must be triggered to rollback previous steps (e.g., restore inventory).

## General Edge Cases
- Network partitions between services.
- Database transient failures (handled via Polly retries).
- Message duplication (handled via Idempotent Consumers and Inbox Pattern).
