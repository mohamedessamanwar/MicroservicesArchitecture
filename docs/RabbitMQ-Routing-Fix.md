# RabbitMQ Routing Issue & Resolution

This document details the configuration bug encountered when integrating Debezium with RabbitMQ and the specific solution applied to fix it.

## The Problem: Data Loss / Missing Messages

Despite all services running correctly (Debezium, RabbitMQ, Order Service), messages captured by Debezium were **not arriving** in the Order Service's queue (`order.write.cdc.q`).

### Root Cause Analysis

1.  **Strict Routing Keys**: The RabbitMQ topology was initially set up with a **Strict Binding**:
    - **Binding**: Exchange `cdc.exchange` -> Queue `order.write.cdc.q` ONLY if `routing_key = order.write`.
    - **Debezium Behavior**: By default, Debezium publishes messages using a hierarchical routing key based on the table structure: `prefix.schema.table` (e.g., `order-server.public.Orders`).
2.  **The Mismatch**:
    - RabbitMQ received a message with key `order-server.public.Orders`.
    - It looked for a queue bound with that EXACT key.
    - It found none (since we only bound `order.write`).
    - **Result**: The message was silently dropped.

## The Solution: Catch-All & Pattern Matching

To solve this, we updated the RabbitMQ bindings in `rabbitmq-init` to use **Wildcards**.

### 1. Diagnostic Fix (What we did first)

We initially bound the queue with the catch-all wildcard `#`:

```bash
routing_key='#'
```

- **Effect**: "Send EVERYTHING to this queue."
- **Result**: It worked immediately—messages arrived.
- **Side Effect**: The _Payment_ queue also started receiving _Order_ messages (because `#` matches everything).

### 2. Final/Production Fix (What we did last)

We refined the binding to be specific but flexible enough to capture Debezium's default keys:

| Component       | Configuration           | Explanation                                                                                                                                   |
| :-------------- | :---------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------- |
| **Old Binding** | `order.write`           | Too strict. Misses Debezium defaults.                                                                                                         |
| **New Binding** | `order-server.public.#` | **Correct.** Captures any event from the order server's public schema (e.g., `order-server.public.Orders`, `order-server.public.OrderLines`). |

### Code Change in `DockerCompose.yaml`

```yaml
# Before (Broken)
rabbitmqadmin ... declare binding ... routing_key=order.write

# After (Fixed)
rabbitmqadmin ... declare binding ... routing_key='order-server.public.#'
```

## Summary

The issue was strictly a **Topology Mismatch**. RabbitMQ exchanges are very literal; if the key doesn't match the binding pattern, the data is discarded. By using the specialized wildcard pattern `order-server.public.#`, we ensure all relevant database changes are routed correctly without leaking data to unrelated queues.
