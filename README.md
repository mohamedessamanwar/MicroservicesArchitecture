# Microservices Architecture Learning Project

## Context
This project is hosted on Git and serves as a comprehensive learning resource for mastering **Microservices Architecture**. It is designed to explore the real-world problems of distributed systems, such as data consistency, network failures, resilient communication, and multi-tenant data isolation.

## System Architecture Overview
The platform consists of **3 Core Microservices** and an **API Gateway**:
1. **Order Service:** Manages the order lifecycle.
2. **Payment Service:** Processes user payments idempotently.
3. **Product Service:** Manages inventory and catalog.
4. **API Gateway (YARP):** Routes traffic to the backend services.

### Communication Patterns
- **Synchronous Communication:** The API Gateway forwards HTTP requests to the microservices.
- **Asynchronous Communication:** Services communicate with each other using **RabbitMQ**. For example, the Order Service publishes an OrderCreated event to a topic exchange, which the Payment and Product services consume to process payments and deduct inventory.
- **Resilience:** HTTP pipelines use Polly for retries and circuit breakers. Async flows use the Inbox/Outbox patterns and Saga Orchestration/Choreography to ensure eventual consistency.

## How to Run Docker Compose
To run the entire system locally:
1. Ensure Docker Desktop is running.
2. Run the following command in the root directory:
   `docker compose up -d`
3. **Inspect Containers:** To see the status of the running microservices and infrastructure:
   `docker compose ps`
4. **View Logs:** If a container is unhealthy or you want to debug an issue:
   `docker compose logs --tail=50 <service_name>` (e.g., docker compose logs order-service)

# 🏗️ Microservices Architecture Diagram — E-Commerce Platform

> A comprehensive visual blueprint of the distributed system architecture, covering all infrastructure components, communication protocols, and architectural patterns.

---

## 1. System Overview — Full Architecture Topology

> [!NOTE]
> **How to read this diagram**: Traffic flows **top-to-bottom** from Client → API Gateway → Microservices → Infrastructure → Data Layer.
> Each subgraph represents a distinct deployment boundary. Arrow labels indicate the **protocol** and **action** of each connection.

```mermaid
%%{init: {
  "theme": "dark",
  "themeVariables": {
    "primaryColor": "#1e293b",
    "primaryTextColor": "#f1f5f9",
    "primaryBorderColor": "#7c3aed",
    "lineColor": "#60a5fa",
    "secondaryColor": "#0f172a",
    "tertiaryColor": "#1e1b4b",
    "noteBkgColor": "#1e1b4b",
    "noteTextColor": "#e2e8f0",
    "noteBorderColor": "#7c3aed"
  }
}}%%

flowchart TD

    %% ═══════════════════════════════════════════
    %% CLIENT LAYER
    %% ═══════════════════════════════════════════
    subgraph CLIENT_LAYER["🌐 CLIENT LAYER"]
        direction LR
        CLIENT["👤 <b>Client / Frontend</b><br/>Web App · Mobile App · SPA<br/>━━━━━━━━━━━━━━━━━━━━<br/>🎯 <i>Entry point for all user traffic</i>"]
    end

    %% ═══════════════════════════════════════════
    %% API GATEWAY LAYER
    %% ═══════════════════════════════════════════
    subgraph GATEWAY_LAYER["🔀 API GATEWAY LAYER"]
        direction LR
        GATEWAY["🛡️ <b>API Gateway — YARP</b><br/>Reverse Proxy · .NET 8<br/>━━━━━━━━━━━━━━━━━━━━<br/>✅ Centralized routing & load balancing<br/>✅ Rate limiting & SSL termination<br/>✅ 5s Activity Timeout protection<br/>✅ Routes external HTTP to internal services"]
    end

    %% ═══════════════════════════════════════════
    %% MICROSERVICES LAYER
    %% ═══════════════════════════════════════════
    subgraph SERVICES_LAYER["⚙️ MICROSERVICES LAYER — Independently Deployable Business Domains"]

        subgraph ORDER_DOMAIN["📦 Order Bounded Context"]
            ORDER["📦 <b>Order Service</b><br/>.NET 8 Web API<br/>━━━━━━━━━━━━━━━━━━━━<br/>Manages order lifecycle<br/>Creates Pending → Confirmed orders<br/>Own DB schema: <code>OrderDb</code>"]
            ORDER_OUTBOX[("📤 <b>order_outbox</b><br/>Outbox Table<br/>━━━━━━━━━━━━━<br/>Stores events in<br/>same ACID Tx")]
            ORDER_INBOX[("📥 <b>order_inbox</b><br/>Inbox Table<br/>━━━━━━━━━━━━━<br/>Idempotent event<br/>consumption")]
        end

        subgraph PAYMENT_DOMAIN["💳 Payment Bounded Context"]
            PAYMENT["💳 <b>Payment Service</b><br/>.NET 8 Web API<br/>━━━━━━━━━━━━━━━━━━━━<br/>Processes charges & refunds<br/>Validates idempotency keys<br/>Own DB schema: <code>PaymentDb</code>"]
            PAYMENT_OUTBOX[("📤 <b>payment_outbox</b><br/>Outbox Table<br/>━━━━━━━━━━━━━<br/>Stores events in<br/>same ACID Tx")]
            PAYMENT_INBOX[("📥 <b>payment_inbox</b><br/>Inbox Table<br/>━━━━━━━━━━━━━<br/>Idempotent event<br/>consumption")]
        end

        subgraph PRODUCT_DOMAIN["🏷️ Product Bounded Context"]
            PRODUCT["🏷️ <b>Product Service</b><br/>.NET 8 Web API<br/>━━━━━━━━━━━━━━━━━━━━<br/>Product catalog & inventory<br/>Stock deduction on purchase<br/>Own DB schema: <code>ProductDb</code>"]
            PRODUCT_OUTBOX[("📤 <b>product_outbox</b><br/>Outbox Table<br/>━━━━━━━━━━━━━<br/>Stores events in<br/>same ACID Tx")]
        end

    end

    %% ═══════════════════════════════════════════
    %% MESSAGING & CACHING INFRASTRUCTURE
    %% ═══════════════════════════════════════════
    subgraph INFRA_LAYER["📡 MESSAGING & CACHING INFRASTRUCTURE"]
        direction LR
        RABBITMQ["🐰 <b>RabbitMQ 3</b><br/>Message Broker / Event Bus<br/>━━━━━━━━━━━━━━━━━━━━<br/>✅ Async non-blocking communication<br/>✅ Decouples services for fault isolation<br/>✅ Durable queues with dead-letter exchange<br/>Protocol: <b>AMQP 0-9-1</b>"]

        REDIS["⚡ <b>Redis 7</b><br/>Distributed Cache<br/>━━━━━━━━━━━━━━━━━━━━<br/>✅ Product catalog cache — low latency reads<br/>✅ Payment idempotency keys — prevents<br/>    double-charging on network retries<br/>✅ Rate limiter token bucket store<br/>Protocol: <b>RESP over TCP</b>"]
    end

    %% ═══════════════════════════════════════════
    %% DATA LAYER
    %% ═══════════════════════════════════════════
    subgraph DATA_LAYER["🗄️ DATA LAYER — PostgreSQL 16 Cluster with CQRS"]

        PGBOUNCER["🔄 <b>PgBouncer</b><br/>Connection Pooling Proxy<br/>━━━━━━━━━━━━━━━━━━━━<br/>✅ Recycles a small pool of DB connections<br/>    across thousands of microservice requests<br/>✅ Prevents DB connection exhaustion<br/>✅ Drastically reduces PostgreSQL memory<br/>    overhead per-connection<br/>Port: <b>6432</b>"]

        subgraph PG_CLUSTER["🐘 PostgreSQL 16 Cluster"]
            PG_PRIMARY[("🔵 <b>PostgreSQL Primary</b><br/>write-db<br/>━━━━━━━━━━━━━<br/>All INSERT / UPDATE / DELETE<br/>WAL Level: logical<br/>Max Replication Slots: 10")]

            PG_REPLICA[("🟢 <b>PostgreSQL Read Replica</b><br/>read-db<br/>━━━━━━━━━━━━━<br/>All SELECT queries<br/>Hot Standby mode<br/>Kept in sync via<br/>Logical Replication")]
        end

    end

    %% ═══════════════════════════════════════════
    %% CONNECTIONS — Top to Bottom Flow
    %% ═══════════════════════════════════════════

    %% Client → Gateway
    CLIENT -->|"HTTPS / REST<br/>External Traffic"| GATEWAY

    %% Gateway → Microservices
    GATEWAY -->|"REST → /api/orders<br/>HTTP/1.1 Internal"| ORDER
    GATEWAY -->|"REST → /api/payments<br/>HTTP/1.1 Internal"| PAYMENT
    GATEWAY -->|"REST → /api/products<br/>HTTP/1.1 Internal"| PRODUCT

    %% Transactional Outbox — Services write to Outbox in same ACID Tx
    ORDER -.->|"ACID Tx:<br/>Save State + Insert Event"| ORDER_OUTBOX
    PAYMENT -.->|"ACID Tx:<br/>Save State + Insert Event"| PAYMENT_OUTBOX
    PRODUCT -.->|"ACID Tx:<br/>Save State + Insert Event"| PRODUCT_OUTBOX

    %% Inbox — Services receive events idempotently
    ORDER_INBOX -.->|"Dedup Check"| ORDER
    PAYMENT_INBOX -.->|"Dedup Check"| PAYMENT

    %% Outbox → RabbitMQ (Background Workers)
    ORDER_OUTBOX -->|"Background Worker<br/>Publishes Event (AMQP)"| RABBITMQ
    PAYMENT_OUTBOX -->|"Background Worker<br/>Publishes Event (AMQP)"| RABBITMQ
    PRODUCT_OUTBOX -->|"Background Worker<br/>Publishes Event (AMQP)"| RABBITMQ

    %% RabbitMQ → Consumers (Saga Choreography)
    RABBITMQ -->|"Consumes: OrderCreated<br/>Charges Customer"| PAYMENT
    RABBITMQ -->|"Consumes: PaymentSucceeded<br/>Confirms Order"| ORDER
    RABBITMQ -->|"Consumes: PaymentSucceeded<br/>Deducts Stock"| PRODUCT

    %% Redis Cache Connections
    PRODUCT -->|"Cache Read/Write<br/>RESP over TCP"| REDIS
    PAYMENT -->|"Idempotency Key<br/>Check/Store (TCP)"| REDIS
    ORDER -->|"Rate Limiter<br/>Token Bucket (TCP)"| REDIS

    %% CQRS — Write Path (Commands) through PgBouncer
    ORDER -->|"Commands — Write<br/>TCP via Port 6432"| PGBOUNCER
    PAYMENT -->|"Commands — Write<br/>TCP via Port 6432"| PGBOUNCER
    PRODUCT -->|"Commands — Write<br/>TCP via Port 6432"| PGBOUNCER
    PGBOUNCER -->|"Pooled Connection<br/>TCP/IP"| PG_PRIMARY

    %% CQRS — Read Path (Queries) to Replica
    ORDER -->|"Queries — Read<br/>TCP Direct"| PG_REPLICA
    PAYMENT -->|"Queries — Read<br/>TCP Direct"| PG_REPLICA
    PRODUCT -->|"Queries — Read<br/>TCP Direct"| PG_REPLICA

    %% Replication
    PG_PRIMARY -->|"Logical Replication<br/>Async WAL Streaming"| PG_REPLICA

    %% ═══════════════════════════════════════════
    %% STYLES
    %% ═══════════════════════════════════════════
    classDef clientStyle fill:#4f46e5,stroke:#818cf8,stroke-width:2px,color:#f1f5f9
    classDef gatewayStyle fill:#7c3aed,stroke:#a78bfa,stroke-width:2px,color:#f1f5f9
    classDef serviceStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef outboxStyle fill:#92400e,stroke:#fbbf24,stroke-width:1px,color:#fef3c7
    classDef brokerStyle fill:#be123c,stroke:#fb7185,stroke-width:2px,color:#fff1f2
    classDef cacheStyle fill:#b45309,stroke:#fbbf24,stroke-width:2px,color:#fef9c3
    classDef poolerStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef dbPrimaryStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe
    classDef dbReplicaStyle fill:#047857,stroke:#34d399,stroke-width:2px,color:#d1fae5
    classDef layerStyle fill:#0f172a,stroke:#334155,stroke-width:2px,color:#94a3b8

    class CLIENT clientStyle
    class GATEWAY gatewayStyle
    class ORDER,PAYMENT,PRODUCT serviceStyle
    class ORDER_OUTBOX,PAYMENT_OUTBOX,PRODUCT_OUTBOX,ORDER_INBOX,PAYMENT_INBOX outboxStyle
    class RABBITMQ brokerStyle
    class REDIS cacheStyle
    class PGBOUNCER poolerStyle
    class PG_PRIMARY dbPrimaryStyle
    class PG_REPLICA dbReplicaStyle

    style CLIENT_LAYER fill:#1e1b4b,stroke:#4f46e5,stroke-width:2px,color:#c7d2fe
    style GATEWAY_LAYER fill:#2e1065,stroke:#7c3aed,stroke-width:2px,color:#c4b5fd
    style SERVICES_LAYER fill:#042f2e,stroke:#0d9488,stroke-width:2px,color:#99f6e4
    style ORDER_DOMAIN fill:#0c4a6e,stroke:#0ea5e9,stroke-width:1px,color:#bae6fd
    style PAYMENT_DOMAIN fill:#4c1d95,stroke:#8b5cf6,stroke-width:1px,color:#ddd6fe
    style PRODUCT_DOMAIN fill:#365314,stroke:#84cc16,stroke-width:1px,color:#d9f99d
    style INFRA_LAYER fill:#450a0a,stroke:#ef4444,stroke-width:2px,color:#fecaca
    style DATA_LAYER fill:#1e1b4b,stroke:#6366f1,stroke-width:2px,color:#c7d2fe
    style PG_CLUSTER fill:#172554,stroke:#3b82f6,stroke-width:1px,color:#93c5fd
```

---

## 2. CQRS Pattern — Command & Query Responsibility Segregation

> [!IMPORTANT]
> **Why CQRS?** In an e-commerce system, **read operations vastly outnumber writes** (browsing products vs. placing orders).
> Splitting reads from writes prevents complex write-locks from slowing down data retrieval and enables independent scaling.

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart LR

    subgraph MICROSERVICE["⚙️ Any Microservice"]
        CMD["✏️ <b>Command Handler</b><br/>INSERT / UPDATE / DELETE<br/>━━━━━━━━━━━━━━━<br/>Mutates State"]
        QRY["🔍 <b>Query Handler</b><br/>SELECT<br/>━━━━━━━━━━━━━━━<br/>Reads State"]
    end

    subgraph WRITE_PATH["✏️ Write Path"]
        PGB_W["🔄 PgBouncer<br/>Port 6432"]
        DB_W[("🔵 PostgreSQL<br/><b>Primary</b><br/>write-db")]
    end

    subgraph READ_PATH["🔍 Read Path"]
        DB_R[("🟢 PostgreSQL<br/><b>Read Replica</b><br/>read-db")]
    end

    CMD -->|"Commands<br/>via Pooled Connection"| PGB_W
    PGB_W -->|"TCP/IP"| DB_W
    QRY -->|"Queries<br/>Direct TCP"| DB_R
    DB_W -->|"Logical Replication<br/>Async WAL Streaming"| DB_R

    classDef cmdStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe
    classDef qryStyle fill:#047857,stroke:#34d399,stroke-width:2px,color:#d1fae5
    classDef poolStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef primaryStyle fill:#1e40af,stroke:#3b82f6,stroke-width:2px,color:#dbeafe
    classDef replicaStyle fill:#065f46,stroke:#10b981,stroke-width:2px,color:#d1fae5

    class CMD cmdStyle
    class QRY qryStyle
    class PGB_W poolStyle
    class DB_W primaryStyle
    class DB_R replicaStyle
```

| Component | Role | Benefit |
|---|---|---|
| **Command Handler** | Routes all write operations (INSERT/UPDATE/DELETE) through PgBouncer to the Primary DB | Ensures data consistency through single-writer semantics |
| **Query Handler** | Routes all read operations (SELECT) directly to the Read Replica | Offloads read-heavy workloads, reducing Primary DB load |
| **PgBouncer** | Connection pooling proxy in front of Primary DB | Recycles connections — prevents exhaustion under high write throughput |
| **Logical Replication** | Async WAL streaming from Primary → Replica | Near-zero lag data sync with minimal performance impact on the Primary |

---

## 3. Saga Pattern — Choreography-Based Distributed Transaction

> [!IMPORTANT]
> **Why Saga?** A traditional 2-Phase Commit (2PC) requires a distributed lock across all services, creating a single point of failure.
> The Saga pattern replaces 2PC with a **chain of local transactions coordinated through events**, enabling each service to fail and compensate independently.

```mermaid
%%{init: {"theme": "dark"}}%%

sequenceDiagram
    autonumber

    participant C as 👤 Client
    participant GW as 🛡️ API Gateway
    participant OS as 📦 Order Service
    participant OB as 📤 Order Outbox
    participant MQ as 🐰 RabbitMQ
    participant PS as 💳 Payment Service
    participant PB as 📤 Payment Outbox
    participant PRS as 🏷️ Product Service

    C->>GW: POST /api/orders (HTTPS)
    GW->>OS: Forward Request (REST)

    rect rgb(30, 58, 138)
        Note over OS,OB: 🔒 Single ACID Transaction
        OS->>OS: Create Order (Status: Pending)
        OS->>OB: INSERT into order_outbox<br/>Event: OrderCreated
    end

    Note over OB,MQ: ⏱️ Background Worker polls Outbox
    OB->>MQ: Publish OrderCreated (AMQP)

    rect rgb(91, 33, 182)
        Note over MQ,PS: 📨 Event Consumption
        MQ->>PS: Deliver OrderCreated
        PS->>PS: Validate & Charge Customer
    end

    rect rgb(30, 58, 138)
        Note over PS,PB: 🔒 Single ACID Transaction
        PS->>PS: Save Payment Record
        PS->>PB: INSERT into payment_outbox<br/>Event: PaymentSucceeded
    end

    Note over PB,MQ: ⏱️ Background Worker polls Outbox
    PB->>MQ: Publish PaymentSucceeded (AMQP)

    par Parallel Event Consumption
        rect rgb(4, 120, 87)
            MQ->>OS: Deliver PaymentSucceeded
            OS->>OS: Update Order → Status: Confirmed ✅
        end
    and
        rect rgb(4, 120, 87)
            MQ->>PRS: Deliver PaymentSucceeded
            PRS->>PRS: Deduct Stock from Inventory 📉
        end
    end

    OS-->>GW: 200 OK — Order Confirmed
    GW-->>C: Response
```

### Saga Flow Summary

| Step | Service | Action | Event Published |
|------|---------|--------|-----------------|
| 1 | **Order Service** | Creates order with `Status: Pending` | `OrderCreated` |
| 2 | **Payment Service** | Consumes `OrderCreated`, charges customer | `PaymentSucceeded` |
| 3 | **Order Service** | Consumes `PaymentSucceeded`, marks order `Confirmed` | — |
| 4 | **Product Service** | Consumes `PaymentSucceeded`, deducts stock | — |

> [!TIP]
> **Compensation**: If payment fails, the Payment Service publishes `PaymentFailed`, and the Order Service rolls back the order to `Cancelled`. No distributed lock required.

---

## 4. Transactional Outbox / Inbox Pattern

> [!CAUTION]
> **The Dual-Write Problem**: If a service saves to the database and then publishes an event to RabbitMQ as two separate operations, a crash between them causes **data inconsistency** — the DB has the data but the event was never published (or vice versa).

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart TD

    subgraph SERVICE["⚙️ Microservice"]
        API["🌐 API Endpoint<br/>Receives Command"]
    end

    subgraph ACID_TX["🔒 Single ACID Transaction"]
        direction LR
        BIZ_TABLE[("📋 <b>Business Table</b><br/>orders / payments<br/>━━━━━━━━━━━━━━━<br/>INSERT business data")]
        OUTBOX_TABLE[("📤 <b>Outbox Table</b><br/>outbox_messages<br/>━━━━━━━━━━━━━━━<br/>INSERT serialized event<br/>Same transaction")]
    end

    WORKER["⏱️ <b>Background Worker</b><br/>Polls Outbox table<br/>every N seconds"]
    
    MQ["🐰 <b>RabbitMQ</b><br/>Message Broker"]
    
    MARK["✅ Mark event as<br/><b>Published</b> in Outbox"]

    subgraph CONSUMER_SVC["⚙️ Consuming Microservice"]
        INBOX[("📥 <b>Inbox Table</b><br/>inbox_messages<br/>━━━━━━━━━━━━━━━<br/>Dedup by MessageId")]
        HANDLER["🔧 <b>Event Handler</b><br/>Processes event<br/>only if not in Inbox"]
    end

    API -->|"1. Execute Command"| BIZ_TABLE
    API -->|"1. Insert Event<br/>(same Tx)"| OUTBOX_TABLE
    WORKER -->|"2. Poll unpublished<br/>events"| OUTBOX_TABLE
    WORKER -->|"3. Publish to<br/>RabbitMQ (AMQP)"| MQ
    WORKER -->|"4. Update status"| MARK
    MARK -.->|"SET published = true"| OUTBOX_TABLE
    MQ -->|"5. Deliver event"| INBOX
    INBOX -->|"6. If new MessageId"| HANDLER

    classDef svcStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef txStyle fill:#1e3a5f,stroke:#3b82f6,stroke-width:2px,color:#dbeafe
    classDef outboxStyle fill:#92400e,stroke:#fbbf24,stroke-width:1px,color:#fef3c7
    classDef workerStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef brokerStyle fill:#be123c,stroke:#fb7185,stroke-width:2px,color:#fff1f2
    classDef markStyle fill:#047857,stroke:#34d399,stroke-width:1px,color:#d1fae5

    class API svcStyle
    class BIZ_TABLE,OUTBOX_TABLE,INBOX outboxStyle
    class WORKER workerStyle
    class MQ brokerStyle
    class MARK markStyle
    class HANDLER svcStyle

    style ACID_TX fill:#0f172a,stroke:#3b82f6,stroke-width:3px,stroke-dasharray: 5 5,color:#93c5fd
    style SERVICE fill:#042f2e,stroke:#0d9488,stroke-width:2px,color:#99f6e4
    style CONSUMER_SVC fill:#042f2e,stroke:#0d9488,stroke-width:2px,color:#99f6e4
```

### Why This Pattern is Critical

| Problem | Outbox/Inbox Solution |
|---|---|
| **Broker down during DB commit** | Event is safely stored in the Outbox table — the background worker retries publishing until successful |
| **Duplicate message delivery** | The Inbox table deduplicates by `MessageId` — each event is processed **exactly once** |
| **Partial failure (crash mid-operation)** | Both the business data and the event are in the **same ACID transaction** — either both commit or neither does |

---

## 5. Component Reference — Benefits & Patterns

### 🌐 Client / Frontend
| Attribute | Detail |
|---|---|
| **Role** | Entry point for all user traffic |
| **Protocols** | HTTPS / REST |
| **Benefit** | Single interface for Web, Mobile, and SPA clients to interact with the platform |

---

### 🛡️ API Gateway (YARP — .NET 8)
| Attribute | Detail |
|---|---|
| **Role** | Centralized reverse proxy & request router |
| **Pattern** | Gateway Routing / Gateway Aggregation |
| **Protocols** | HTTPS (external) → HTTP (internal) |
| **Key Features** | Route-based forwarding, rate limiting, SSL termination, 5s activity timeout |
| **Benefit** | Clients call a single URL; the gateway handles discovery, routing, and cross-cutting concerns like auth and throttling |

---

### ⚙️ Microservices (Order · Payment · Product)
| Attribute | Detail |
|---|---|
| **Role** | Independently deployable business domains |
| **Pattern** | Database-per-Service (each has its own schema) |
| **Runtime** | .NET 8, Kestrel, Docker containers |
| **Benefit** | Teams can develop, deploy, and scale each service independently without cross-service coordination |

---

### 📊 CQRS (Command Query Responsibility Segregation)
| Attribute | Detail |
|---|---|
| **Write Path** | Service → PgBouncer (port 6432) → PostgreSQL Primary |
| **Read Path** | Service → PostgreSQL Read Replica (direct) |
| **Benefit** | Separates read-heavy catalog browsing from write-heavy order processing, enabling independent optimization and scaling of each path |

---

### 🐘 PostgreSQL 16 Cluster
| Component | Role | Detail |
|---|---|---|
| **Primary (write-db)** | All writes | Handles INSERT/UPDATE/DELETE, WAL level: logical, 10 replication slots |
| **Read Replica (read-db)** | All reads | Hot standby, synced via async logical replication |
| **Benefit** | High availability + horizontal scaling of reads without impacting write performance |

---

### 🔄 PgBouncer (Connection Pooler)
| Attribute | Detail |
|---|---|
| **Position** | Sits between microservices and PostgreSQL Primary |
| **Port** | 6432 |
| **Pooling Mode** | Transaction-level pooling |
| **Benefit** | Recycles a small pool of active DB connections across thousands of concurrent microservice requests, preventing PostgreSQL connection exhaustion and drastically reducing per-connection memory overhead |

---

### 🐰 RabbitMQ 3 (Message Broker)
| Attribute | Detail |
|---|---|
| **Protocol** | AMQP 0-9-1 |
| **Features** | Durable queues, dead-letter exchanges, management UI |
| **Pattern** | Event Bus for choreography-based Saga |
| **Benefit** | Enables asynchronous, non-blocking inter-service communication. If one service goes down, others continue operating — messages queue up and are processed when the service recovers |

---

### ⚡ Redis 7 (Distributed Cache)
| Use Case | Detail |
|---|---|
| **Product Catalog Cache** | Caches frequent product reads to reduce DB latency |
| **Idempotency Keys** | Payment Service stores keys to prevent double-charging on network retries |
| **Rate Limiter Store** | Stores token bucket state for per-user/IP rate limiting (Lua script for atomicity) |
| **Benefit** | Sub-millisecond reads for hot data, protecting both the database and the customer's wallet |

---

### 📤📥 Transactional Outbox / Inbox
| Attribute | Detail |
|---|---|
| **Outbox** | Events inserted in the same ACID transaction as business data |
| **Inbox** | Deduplicates incoming events by `MessageId` |
| **Worker** | Background process polls Outbox and publishes to RabbitMQ |
| **Benefit** | Guarantees exactly-once message publishing, eliminating the dual-write problem and preventing data inconsistencies if the broker is unavailable |

---

### 🔄 Saga Pattern (Choreography)
| Attribute | Detail |
|---|---|
| **Style** | Choreography (no central orchestrator) |
| **Flow** | `OrderCreated` → `PaymentSucceeded` → Order confirmed + Stock deducted |
| **Compensation** | `PaymentFailed` → Order cancelled (rollback) |
| **Benefit** | Manages distributed transactions across multiple services without a distributed lock or 2PC, maintaining eventual consistency with full fault tolerance |

---

## 6. Network & Container Topology

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart LR

    subgraph DOCKER["🐳 Docker Compose — microservices-network (bridge)"]
        direction TB

        subgraph EXTERNAL["🌍 External Ports"]
            P80["Port 80"]
            P443["Port 443"]
        end

        GW["🛡️ api-gateway<br/>:8080"]
        OS["📦 order-service<br/>:8080"]
        PS["💳 payment-service<br/>:8080"]
        PRS["🏷️ product-service<br/>:8080"]
        PGB["🔄 pgbouncer<br/>:6432"]
        WDB[("🔵 write-db<br/>:5432")]
        RDB[("🟢 read-db<br/>:5432")]
        RMQ["🐰 rabbitmq<br/>:5672 / :15672"]
        RD["⚡ redis<br/>:6379"]

        P80 --> GW
        P443 --> GW
        GW --> OS
        GW --> PS
        GW --> PRS
        OS & PS & PRS --> PGB
        OS & PS & PRS --> RDB
        OS & PS & PRS --> RMQ
        OS & PS & PRS --> RD
        PGB --> WDB
        WDB --> RDB
    end

    classDef extStyle fill:#7c3aed,stroke:#a78bfa,stroke-width:2px,color:#f1f5f9
    classDef gwStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef svcStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef dbStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe
    classDef infraStyle fill:#be123c,stroke:#fb7185,stroke-width:2px,color:#fff1f2

    class P80,P443 extStyle
    class GW gwStyle
    class OS,PS,PRS svcStyle
    class PGB,WDB,RDB dbStyle
    class RMQ,RD infraStyle
```

---

> [!TIP]
> **Color Legend**: 🔵 Blue = Databases | 🟢 Green = Read Replicas | 🟣 Purple = Gateway/Proxy | 🔴 Red = Message Broker | 🟠 Orange = Cache/Outbox | 🟤 Teal = Microservices

---
---

# 🚀 Project Run & API Guide

> This guide covers everything you need to **build, run, monitor, and test** the entire microservices platform — including Docker commands, EF Core migrations, API endpoints, connection strings, and operational deep-dives.

---

## 7. EF Core Migrations & Connection Strings

### Generating a SQL Migration Script

To export your EF Core migrations to a raw SQL file (useful for applying to a running database or for code review):

```bash
dotnet ef migrations script -o migration.sql
```

If you need to specify the infrastructure and API projects explicitly:

```bash
dotnet ef migrations script \
  --project ./OrderService.Infrastructure \
  --startup-project ./OrderService.Api \
  -o OrderService_Migration.sql
```

### Does `add migration` Require a Valid Connection String?

> [!NOTE]
> **No.** When you run `dotnet ef migrations add <Name>`, EF Core only analyzes your C# code (`DbContext` and Entity classes) and compares it with your current `ModelSnapshot.cs`. It does **not** establish a connection to the physical database.

The only caveat is if your `OnConfiguring` or `Startup` code throws a fatal exception when the connection string is literally `null`. As long as the string exists (even if it's a placeholder like `Server=fake`), the migration generation will succeed.

**You only need a valid, reachable connection string during the Update phase:**

```bash
dotnet ef database update
# OR by applying the generated SQL script directly
```

---

## 8. Docker Compose Commands Explained

Understanding the difference between Docker Compose commands is critical for local development:

### `docker compose build`

```bash
docker compose build
```

| What it does | Reads your `Dockerfile`s and compiles your source code into Docker Images |
|---|---|
| **When to use** | When you only want to prepare/build the images but **don't** want to start the containers yet |

---

### `docker compose up`

```bash
docker compose up
```

| What it does | Starts your containers based on **existing** images. If images don't exist locally, it builds them first |
|---|---|
| **⚠️ Important** | If you change your C# code and run `docker compose up` again, it will **NOT** recompile your code if the image already exists. It will start the **old version** |

---

### `docker compose up --build`

```bash
docker compose up --build
```

| What it does | Forces Docker to **completely rebuild** the images from your latest source code before starting the containers |
|---|---|
| **When to use** | **Every time** you modify your C# code and want to test the new changes locally |

---

### Quick Reference

| Command | Builds Images? | Starts Containers? | Picks Up Code Changes? |
|---|:---:|:---:|:---:|
| `docker compose build` | ✅ | ❌ | ✅ |
| `docker compose up` | Only if missing | ✅ | ❌ |
| `docker compose up --build` | ✅ (forced) | ✅ | ✅ |

---

## 9. How to Run the Solution

### Prerequisites
1. **Docker Desktop** must be installed and **running**
2. Open your terminal in the **root directory** (where [docker-compose.yml](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/docker-compose.yml) is located)

### Start Everything

Build the latest code and start all 8 services in detached (background) mode:

```bash
docker compose up --build -d
```

### Verify Services Are Healthy

Check that all containers have reached `healthy` status:

```bash
docker compose ps
```

### View API Logs

Stream live logs from the gateway and business services:

```bash
docker compose logs -f api-gateway order-service payment-service product-service
```

### Stop Everything

```bash
docker compose down
```

To also **remove all volumes** (wipes databases, Redis, RabbitMQ data):

```bash
docker compose down -v
```

---

## 10. Docker & Container Management — Monitoring, Logs, Bash

### Checking Container Status

See the status of all services (running, restarting, or exited):

```bash
docker compose ps
```

### Checking CPU & Memory Usage

See real-time statistics (CPU, Memory, Network I/O) for all running containers:

```bash
docker stats
```

> Press `Ctrl+C` to exit this view.

### Viewing Logs

| Command | Description |
|---|---|
| `docker compose logs -f` | Stream logs from **all** services at once |
| `docker compose logs -f order-service` | Stream logs from a **specific** service |
| `docker compose logs -f --tail=100 payment-service` | Show the last 100 lines + stream new logs |

> The `-f` flag "follows" the logs (streams live). Press `Ctrl+C` to stop watching.

### Accessing a Container's Terminal (Shell)

Execute commands **inside** a running container:

```bash
docker compose exec <service-name> sh
```

**Examples:**
```bash
docker compose exec write-db sh          # PostgreSQL Primary shell
docker compose exec redis redis-cli      # Redis CLI directly
docker compose exec rabbitmq rabbitmqctl status   # RabbitMQ status
```

> [!TIP]
> Lightweight alpine-based containers (like the .NET APIs) only have `sh`, not `bash`. If the container has `bash`, you can replace `sh` with `bash`.

---

## 11. API Endpoints & Gateway Routing

The [api-gateway](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/ApiGateway/appsettings.json) exposes port `5000` (and `80`/`443`) to your local machine. It uses **YARP** to route requests to internal microservices:

| Route Pattern | Target Service | Internal Address |
|---|---|---|
| `/api/v1/Orders/**` | `order-service` | `http://order-service:8080` |
| `/api/v1/Payments/**` | `payment-service` | `http://payment-service:8080` |
| `/api/v1/Products/**` | `product-service` | `http://product-service:8080` |

### Required Headers

| Header | Required For | Value |
|---|---|---|
| `Content-Type` | Any `POST` or `PUT` with a body | `application/json` |
| `X-Country` | All requests (multi-tenancy routing) | `Egypt` or `USA` |
| `X-Correlation-ID` | Distributed tracing (optional but recommended) | Any GUID |

### Request Flow Diagram

```mermaid
%%{init: {"theme": "dark"}}%%

sequenceDiagram
    autonumber
    participant C as 👤 Client / Postman
    participant GW as 🛡️ API Gateway<br/>localhost:5000
    participant OS as 📦 Order Service<br/>:8080
    participant PS as 💳 Payment Service<br/>:8080
    participant PRS as 🏷️ Product Service<br/>:8080

    rect rgb(30, 58, 138)
        Note over C,GW: All requests go through the Gateway
        C->>GW: GET /api/v1/Orders<br/>X-Country: Egypt
        GW->>OS: YARP forwards to order-service:8080
        OS-->>GW: 200 OK (JSON)
        GW-->>C: Response
    end

    rect rgb(91, 33, 182)
        C->>GW: POST /api/v1/Orders<br/>X-Country: USA
        GW->>OS: YARP forwards to order-service:8080
        OS-->>GW: 201 Created
        GW-->>C: Response with Order ID
    end

    rect rgb(4, 120, 87)
        C->>GW: POST /api/v1/Payments<br/>X-Country: Egypt
        GW->>PS: YARP forwards to payment-service:8080
        PS-->>GW: 201 Created
        GW-->>C: Response with Payment ID
    end
```

---

### Orders API (`order-service`)

| Method | Endpoint | Description | Body |
|:---:|---|---|---|
| `GET` | `http://localhost:5000/api/v1/Orders` | Retrieves all orders for the tenant specified in `X-Country` | — |
| `POST` | `http://localhost:5000/api/v1/Orders` | Creates a new order (writes to Primary DB + Outbox) | ✅ JSON |
| `POST` | `http://localhost:5000/api/v1/Orders/{id}/process-payment` | Triggers payment processing for a specific order ID | — |

**Sample POST Body — Create Order:**

```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "totalAmount": 250.75
}
```

---

### Payments API (`payment-service`)

| Method | Endpoint | Description | Body |
|:---:|---|---|---|
| `POST` | `http://localhost:5000/api/v1/Payments` | Creates a new payment record | ✅ JSON |
| `PUT` | `http://localhost:5000/api/v1/Payments/{id}/status?status={status}` | Updates payment status (`0`=Pending, `1`=Completed, `2`=Failed) | — |

**Sample POST Body — Create Payment:**

```json
{
  "orderId": "1c954624-7f83-46e9-b8df-5457857272d6",
  "amount": 250.75
}
```

---

### Health & Observability Endpoints

| Endpoint | Service | Description |
|---|---|---|
| `GET http://localhost:5000/health` | API Gateway | Gateway health check |
| `GET http://localhost:5000/metrics` | API Gateway | OpenTelemetry Prometheus metrics |
| `GET http://localhost:8080/health/live` | Order Service (direct) | Liveness probe |
| `GET http://localhost:8082/health/live` | Payment Service (direct) | Liveness probe |

---

### Postman Collection

A ready-to-import Postman collection is available at [Microservices_API.postman_collection.json](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Microservices_API.postman_collection.json).

**Collection Variables:**

| Variable | Value | Description |
|---|---|---|
| `gatewayUrl` | `http://localhost:5000` | API Gateway base URL |
| `orderServiceDirectUrl` | `http://localhost:8080` | Direct Order Service (bypasses gateway) |
| `paymentServiceDirectUrl` | `http://localhost:8082` | Direct Payment Service (bypasses gateway) |

---

## 12. Multi-Tenant Architecture & Full Request Flow

> [!IMPORTANT]
> This platform supports **multi-tenancy** via the `X-Country` header. Each tenant (`Egypt`, `USA`) has its own isolated database schemas routed through PgBouncer, and its own RabbitMQ queues with dedicated Dead Letter Exchanges.

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart TB

    subgraph ClientLayer["🌐 Clients & Ingress"]
        Client["👤 Web / Mobile / Postman / cURL"]
    end

    subgraph GatewayLayer["🛡️ Edge Layer"]
        Gateway["🛡️ <b>API Gateway — YARP</b><br/>Ports: 5000, 80, 443<br/>━━━━━━━━━━━━━━━━━━━━<br/>• Correlation ID Propagation<br/>• Route Matching: /api/v1/...<br/>• 5s Activity Timeout"]
    end

    subgraph ServiceLayer["⚙️ Application Microservices (.NET 8)"]
        OrderSvc["📦 <b>Order Service</b><br/>━━━━━━━━━━━━━━━━━━━━<br/>• Order Lifecycle Management<br/>• Outbox Dispatcher Background Job<br/>• OrderCreated Event Consumer<br/>• Multi-Tenancy via X-Country<br/>• OpMode Middleware"]
        PaymentSvc["💳 <b>Payment Service</b><br/>━━━━━━━━━━━━━━━━━━━━<br/>• Payment Processing & Status<br/>• Order Synchronization<br/>• Idempotent Payment Handlers<br/>• Multi-Tenancy via X-Country"]
    end

    subgraph CacheAndBroker["📡 State, Cache & Event Broker"]
        Redis[("⚡ <b>Redis 7</b><br/>Port: 6379<br/>━━━━━━━━━━━━━<br/>Distributed Cache<br/>Rate Limiter Store<br/>Idempotency Keys")]
        RabbitMQ["🐰 <b>RabbitMQ 3.x</b><br/>Ports: 5672 / 15672 (UI)<br/>━━━━━━━━━━━━━━━━━━━━<br/>• Topic Exchanges per tenant<br/>• Egypt.order.Q / USA.order.Q<br/>• Dead Letter Exchanges (DLX/DLQ)"]
    end

    subgraph DataAccessLayer["🔄 Connection Pooling"]
        PgBouncer["🔄 <b>PgBouncer</b><br/>Port: 6432<br/>━━━━━━━━━━━━━━━━━━━━<br/>• Transaction Pooling<br/>• max_client_conn: 500<br/>• default_pool_size: 20<br/>• Routes to Write/Read per tenant"]
    end

    subgraph DatabaseLayer["🗄️ PostgreSQL 16 High Availability"]
        WriteDB[("🔵 <b>PostgreSQL Primary</b><br/>write-db · Port: 5432<br/>━━━━━━━━━━━━━<br/>OrderDb · PaymentDb · ProductDb<br/>OrderDb-USA · PaymentDb-USA · ProductDb-USA<br/>Replication Source")]
        ReadDB[("🟢 <b>PostgreSQL Standby Replica</b><br/>read-db · Port: 5432<br/>━━━━━━━━━━━━━<br/>Physical Streaming Replication<br/>Read-Only Queries<br/>Hot Standby Mode")]
    end

    %% Connections
    Client -->|"HTTP / REST<br/>X-Country: Egypt ❘ USA"| Gateway

    Gateway -->|"/api/v1/Orders"| OrderSvc
    Gateway -->|"/api/v1/Payments"| PaymentSvc

    OrderSvc <-->|"Cache / Locks"| Redis
    PaymentSvc <-->|"Cache / Locks"| Redis

    OrderSvc -->|"Publish Outbox Events<br/>(AMQP)"| RabbitMQ
    RabbitMQ -->|"Consume Order Events"| OrderSvc
    PaymentSvc -.->|"Sync Order State"| OrderSvc

    OrderSvc -->|"DB Queries<br/>(via ConnResolver)"| PgBouncer
    PaymentSvc -->|"DB Queries<br/>(via ConnResolver)"| PgBouncer

    PgBouncer -->|"Writes<br/>(POST/PUT/DELETE)"| WriteDB
    PgBouncer -->|"Reads<br/>(GET/HEAD)"| ReadDB

    WriteDB ==>|"Streaming WAL<br/>Replication"| ReadDB

    %% Styles
    classDef clientStyle fill:#4f46e5,stroke:#818cf8,stroke-width:2px,color:#f1f5f9
    classDef gatewayStyle fill:#7c3aed,stroke:#a78bfa,stroke-width:2px,color:#f1f5f9
    classDef serviceStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef cacheStyle fill:#b45309,stroke:#fbbf24,stroke-width:2px,color:#fef9c3
    classDef brokerStyle fill:#be123c,stroke:#fb7185,stroke-width:2px,color:#fff1f2
    classDef poolerStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef primaryStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe
    classDef replicaStyle fill:#047857,stroke:#34d399,stroke-width:2px,color:#d1fae5

    class Client clientStyle
    class Gateway gatewayStyle
    class OrderSvc,PaymentSvc serviceStyle
    class Redis cacheStyle
    class RabbitMQ brokerStyle
    class PgBouncer poolerStyle
    class WriteDB primaryStyle
    class ReadDB replicaStyle

    style ClientLayer fill:#1e1b4b,stroke:#4f46e5,stroke-width:2px,color:#c7d2fe
    style GatewayLayer fill:#2e1065,stroke:#7c3aed,stroke-width:2px,color:#c4b5fd
    style ServiceLayer fill:#042f2e,stroke:#0d9488,stroke-width:2px,color:#99f6e4
    style CacheAndBroker fill:#450a0a,stroke:#ef4444,stroke-width:2px,color:#fecaca
    style DataAccessLayer fill:#2e1065,stroke:#7c3aed,stroke-width:2px,color:#c4b5fd
    style DatabaseLayer fill:#1e1b4b,stroke:#6366f1,stroke-width:2px,color:#c7d2fe
```

### Multi-Tenant Database Routing via PgBouncer

PgBouncer's [pgbouncer.ini](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/pgbouncer/pgbouncer.ini) maps logical database names to physical PostgreSQL instances:

| PgBouncer Database Name | Routes To | Physical DB |
|---|---|---|
| `OrderDb` | `write-db:5432` | OrderDb (Egypt — Primary) |
| `OrderDbReplica` | `read-db:5432` | OrderDb (Egypt — Replica) |
| `OrderDb-USA` | `write-db:5432` | OrderDb-USA (Primary) |
| `OrderDb-USAReplica` | `read-db:5432` | OrderDb-USA (Replica) |
| `PaymentDb` | `write-db:5432` | PaymentDb (Egypt — Primary) |
| `PaymentDbReplica` | `read-db:5432` | PaymentDb (Egypt — Replica) |
| `PaymentDb-USA` | `write-db:5432` | PaymentDb-USA (Primary) |
| `PaymentDb-USAReplica` | `read-db:5432` | PaymentDb-USA (Replica) |
| `ProductDb` | `write-db:5432` | ProductDb (Egypt — Primary) |
| `ProductDbReplica` | `read-db:5432` | ProductDb (Egypt — Replica) |
| `ProductDb-USA` | `write-db:5432` | ProductDb-USA (Primary) |
| `ProductDb-USAReplica` | `read-db:5432` | ProductDb-USA (Replica) |

### RabbitMQ Topology per Tenant

Each tenant gets its own exchange, queue, and dead-letter infrastructure (declared idempotently by [init-topology.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/rabbitmq/init-topology.sh)):

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart LR

    subgraph SHARED["🌍 Shared"]
        SE["order.exchange<br/>(topic)"]
    end

    subgraph EGYPT["🇪🇬 Egypt Tenant"]
        EE["Egypt.order.exchange<br/>(topic)"]
        EQ["Egypt.order.Q<br/>(durable)"]
        EDLX["Egypt.order.Q.dead-letter-exchange<br/>(direct)"]
        EDLQ["Egypt.order.Q.dlq"]
    end

    subgraph USA["🇺🇸 USA Tenant"]
        UE["USA.order.exchange<br/>(topic)"]
        UQ["USA.order.Q<br/>(durable)"]
        UDLX["USA.order.Q.dead-letter-exchange<br/>(direct)"]
        UDLQ["USA.order.Q.dlq"]
    end

    SE -->|"routing: Egypt.order.#"| EQ
    SE -->|"routing: USA.order.#"| UQ
    EE -->|"routing: Egypt.order.# / #"| EQ
    UE -->|"routing: USA.order.# / #"| UQ
    EQ -->|"on reject/expire"| EDLX
    EDLX --> EDLQ
    UQ -->|"on reject/expire"| UDLX
    UDLX --> UDLQ

    classDef sharedStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef egyptStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef usaStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe
    classDef dlqStyle fill:#be123c,stroke:#fb7185,stroke-width:1px,color:#fff1f2

    class SE sharedStyle
    class EE,EQ egyptStyle
    class UE,UQ usaStyle
    class EDLX,EDLQ,UDLX,UDLQ dlqStyle

    style SHARED fill:#1e1b4b,stroke:#7c3aed,stroke-width:2px,color:#c4b5fd
    style EGYPT fill:#042f2e,stroke:#0d9488,stroke-width:2px,color:#99f6e4
    style USA fill:#172554,stroke:#3b82f6,stroke-width:2px,color:#93c5fd
```

---

## 13. Startup Scripts (`.sh`) — Lifecycle & Idempotency Deep Dive

### When Do Startup Scripts Run?

> [!IMPORTANT]
> The scripts run on **every container start** (including `docker compose up`, `docker start`, `docker restart`, and container reboots) because they are specified as the container's `entrypoint` (PID 1 process).

**If the attached volume already contains data, does the script run?**
Yes. Docker starts the entrypoint script whenever the container boots. However, the scripts are written to **inspect existing data** and **skip destructive actions**.

### How Each Script Ensures Idempotency

#### [write-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/write-startup.sh) — PostgreSQL Primary

| Operation | Idempotency Guard |
|---|---|
| **Database Creation** | `SELECT 'CREATE DATABASE "OrderDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb')\gexec` — If the database exists, nothing is executed |
| **Replication Role** | `IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'replicator')` — Skips if role already exists |
| **pg_hba.conf Rules** | `grep -q "host all all all scram-sha-256" ... \|\| echo ...` — Only appends if not already present |
| **Publications** | `IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'order_pub')` — Skips if publication exists |
| **EF Core Migrations** | Checks the `__EFMigrationsHistory` table — Already-applied migrations are skipped |

#### [read-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/read-startup.sh) — PostgreSQL Standby Replica

| Operation | Idempotency Guard |
|---|---|
| **Volume Check** | `if [ ! -s "$PGDATA/PG_VERSION" ]; then ... pg_basebackup ... else ... "Assuming replica is already initialized" fi` |
| **Effect** | If data exists in the volume (`PG_VERSION` is present), `pg_basebackup` is **completely skipped** — preventing replica corruption or data overwrites |

#### [init-topology.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/rabbitmq/init-topology.sh) — RabbitMQ

| Operation | Idempotency Guard |
|---|---|
| **Exchange/Queue/Binding Declarations** | Uses `rabbitmqadmin declare ...` — AMQP declarations are **declarative and idempotent by specification**. Declaring existing queues/exchanges does not alter or erase existing queued messages |

```mermaid
%%{init: {"theme": "dark"}}%%

stateDiagram-v2
    [*] --> ContainerStart: docker compose up

    ContainerStart --> CheckVolume: Entrypoint script runs

    state CheckVolume <<choice>>
    CheckVolume --> FreshInit: Volume empty
    CheckVolume --> SkipInit: Volume has data

    FreshInit --> CreateDBs: Create databases idempotently
    CreateDBs --> CreateRoles: Create replication roles
    CreateRoles --> RunMigrations: Apply EF Core migrations
    RunMigrations --> CreatePubs: Create publications
    CreatePubs --> Ready: ✅ PostgreSQL Ready

    SkipInit --> VerifyOnly: Skip destructive operations
    VerifyOnly --> Ready: ✅ PostgreSQL Ready
```

---

## 14. Running Services — Health Status

All **8 containers** are orchestrated via Docker Compose with health checks:

| Service | Container Name | Host Port(s) | Health Probe | Expected Status |
|---|---|---|---|---|
| 🛡️ API Gateway | `api-gateway` | `5000`, `80`, `443` | `GET /health` | ✅ Healthy |
| 📦 Order Service | `order-service` | `8080`, `4024` | `GET /health/live` | ✅ Healthy |
| 💳 Payment Service | `payment-service` | `8082` | `GET /health/live` | ✅ Healthy |
| 🏷️ Product Service | `product-service` | `8084` | `GET /health/live` | ✅ Healthy |
| 🔄 PgBouncer | `pgbouncer` | `6432` | `pg_isready -p 6432` | ✅ Healthy |
| 🔵 PostgreSQL Primary | `write-db` | `5432` | `pg_isready -d write_db` | ✅ Healthy |
| 🟢 PostgreSQL Replica | `read-db` | `5433` | `select pg_is_in_recovery()` | ✅ Healthy |
| ⚡ Redis | `redis` | `6379` | `redis-cli ping` | ✅ Healthy |
| 🐰 RabbitMQ | `rabbitmq` | `5672`, `15672` | `rabbitmq-diagnostics ping` | ✅ Healthy |

> [!TIP]
> The `rabbitmq-init` container runs once and exits after declaring the RabbitMQ topology. It is expected to show `Exited (0)` status.

---

## 15. Database & Redis Connection Strings — For IDEs & GUI Tools

All host ports are mapped in [docker-compose.override.yml](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/docker-compose.override.yml) so you can connect directly from **DBeaver**, **DataGrip**, **pgAdmin**, **VS Code**, or **Redis Insight**.

---

### 🔄 A. PostgreSQL via PgBouncer (Recommended)

PgBouncer runs on port `6432` and routes dynamically to Primary or Replica based on the database name:

| Parameter | Value |
|---|---|
| **Host** | `localhost` or `127.0.0.1` |
| **Port** | `6432` |
| **User** | `admin` |
| **Password** | `pass` |

**Available Databases:**

| Database | Tenant | Target |
|---|---|---|
| `OrderDb` | Egypt | Primary (write-db) |
| `OrderDbReplica` | Egypt | Replica (read-db) |
| `PaymentDb` | Egypt | Primary (write-db) |
| `PaymentDbReplica` | Egypt | Replica (read-db) |
| `ProductDb` | Egypt | Primary (write-db) |
| `ProductDbReplica` | Egypt | Replica (read-db) |
| `OrderDb-USA` | USA | Primary (write-db) |
| `OrderDb-USAReplica` | USA | Replica (read-db) |
| `PaymentDb-USA` | USA | Primary (write-db) |
| `PaymentDb-USAReplica` | USA | Replica (read-db) |
| `ProductDb-USA` | USA | Primary (write-db) |
| `ProductDb-USAReplica` | USA | Replica (read-db) |

**PostgreSQL URI:**
```
postgresql://admin:pass@localhost:6432/OrderDb
postgresql://admin:pass@localhost:6432/PaymentDb
postgresql://admin:pass@localhost:6432/ProductDb
postgresql://admin:pass@localhost:6432/OrderDb-USA
postgresql://admin:pass@localhost:6432/PaymentDb-USA
postgresql://admin:pass@localhost:6432/ProductDb-USA
```

**ADO.NET / .NET Connection String:**
```
Host=localhost;Port=6432;Database=OrderDb;Username=admin;Password=pass;
Host=localhost;Port=6432;Database=PaymentDb;Username=admin;Password=pass;
Host=localhost;Port=6432;Database=ProductDb;Username=admin;Password=pass;
Host=localhost;Port=6432;Database=OrderDb-USA;Username=admin;Password=pass;
Host=localhost;Port=6432;Database=PaymentDb-USA;Username=admin;Password=pass;
Host=localhost;Port=6432;Database=ProductDb-USA;Username=admin;Password=pass;
```

---

### 🔵 B. Direct PostgreSQL Nodes (Without PgBouncer)

**Primary Database (`write-db`):**

| Parameter | Value |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| User | `admin` |
| Password | `pass` |

```
postgresql://admin:pass@localhost:5432/OrderDb
```

**Read Replica (`read-db`):**

| Parameter | Value |
|---|---|
| Host | `localhost` |
| Port | `5433` |
| User | `admin` |
| Password | `pass` |

```
postgresql://admin:pass@localhost:5433/OrderDb
```

> [!WARNING]
> The Read Replica is in **Hot Standby** mode. You can only run `SELECT` queries against it. Any `INSERT`/`UPDATE`/`DELETE` will be rejected.

---

### ⚡ C. Redis (For Redis Insight / VS Code Extension)

| Parameter | Value |
|---|---|
| Host | `localhost` or `127.0.0.1` |
| Port | `6379` |
| Password | *(none / empty)* |

```
Redis URI:                redis://localhost:6379
.NET StackExchange.Redis: localhost:6379,abortConnect=false
```

---

### 🐰 D. RabbitMQ — Management UI & Broker

| Resource | URL / Value |
|---|---|
| **Management Web UI** | [http://localhost:15672](http://localhost:15672/) |
| **Username** | `admin` |
| **Password** | `admin123` |
| **AMQP Broker URL** | `amqp://admin:admin123@localhost:5672/` |

> [!TIP]
> The RabbitMQ Management UI at [http://localhost:15672](http://localhost:15672/) lets you inspect exchanges, queues, bindings, message rates, and consumer status in real-time. Navigate to **Queues** → `Egypt.order.Q` or `USA.order.Q` to see pending messages and dead-letter counts.

---
---

# 🔌 HTTP Client & Polly Resilience Patterns

> This section documents the inter-service HTTP communication framework built in [Micro.Shared/Http](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http), including the Polly resilience pipelines, the request pipeline chain, and the step-by-step guide to add a new service client.

---

## 16. Resilient HTTP Client Architecture

### Why Typed HttpClients with Polly?

In a microservices architecture, services communicate via HTTP. Without resilience, a single downstream timeout or 500 error can cascade and bring down the entire system. This framework solves that by wrapping every outbound HTTP call with a **layered defense pipeline** using Polly.

### Architecture Overview

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart TD

    subgraph CALLER["⚙️ Calling Microservice (e.g. Order Service)"]
        BIZ["🔧 Business Logic<br/>e.g. ProcessOrderPayment"]
        CLIENT["📡 Typed Service Client<br/>e.g. PaymentServiceClient"]
        BASE["🏗️ DownstreamApiClientBase<br/>SendAsync — sets pipeline key,<br/>idempotency key, JSON serialization"]
    end

    subgraph PIPELINE["🛡️ HTTP Message Handler Pipeline"]
        direction TB
        HEADER["📋 <b>HeaderPropagationHandler</b><br/>━━━━━━━━━━━━━━━━━━━━<br/>Propagates: Authorization,<br/>X-Correlation-Id, X-Country<br/>Adds: X-App-Id, X-App-Signature"]
        POLLY["🔄 <b>Polly Policy Handler</b><br/>━━━━━━━━━━━━━━━━━━━━<br/>Resolves pipeline key from request<br/>Applies: Bulkhead → Circuit Breaker<br/>→ Retry → Timeout"]
        SOCKET["🔌 <b>SocketsHttpHandler</b><br/>━━━━━━━━━━━━━━━━━━━━<br/>Connection pooling, DNS rotation,<br/>TCP connect timeout"]
    end

    subgraph TARGET["🎯 Downstream Microservice"]
        DOWNSTREAM["💳 Payment Service<br/>🏷️ Product Service<br/>📦 Order Service"]
    end

    BIZ -->|"Calls method"| CLIENT
    CLIENT -->|"Sets pipeline key<br/>+ idempotency key"| BASE
    BASE -->|"HttpClient.SendAsync"| HEADER
    HEADER -->|"Enriched Request"| POLLY
    POLLY -->|"Resilient Request"| SOCKET
    SOCKET -->|"HTTP over TCP"| DOWNSTREAM

    classDef callerStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef pipelineStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef socketStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe
    classDef targetStyle fill:#be123c,stroke:#fb7185,stroke-width:2px,color:#fff1f2

    class BIZ,CLIENT,BASE callerStyle
    class HEADER,POLLY pipelineStyle
    class SOCKET socketStyle
    class DOWNSTREAM targetStyle

    style CALLER fill:#042f2e,stroke:#0d9488,stroke-width:2px,color:#99f6e4
    style PIPELINE fill:#2e1065,stroke:#7c3aed,stroke-width:2px,color:#c4b5fd
    style TARGET fill:#450a0a,stroke:#ef4444,stroke-width:2px,color:#fecaca
```

### How the Pipeline Resolves Which Polly Policy to Use

Each outbound request carries a **pipeline key** that determines which resilience settings to apply. The key is resolved in this priority order:

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart TD
    REQ["📨 Outbound HTTP Request"]
    
    CHECK1{"Pipeline key explicitly<br/>set on request.Options?"}
    CHECK2{"What is the HTTP Method?"}
    
    READ["🔍 <b>Read Pipeline</b><br/>Key: read"]
    HEALTH["❤️ <b>Health Pipeline</b><br/>Key: health"]
    CRITICAL["⚠️ <b>Critical Pipeline</b><br/>Key: critical"]
    WRITE["✏️ <b>Write Pipeline</b><br/>Key: write"]
    NORETRY["🚫 <b>NoRetry Pipeline</b><br/>Key: no-retry"]

    REQ --> CHECK1
    CHECK1 -->|"Yes — use that key<br/>(e.g. NoRetry, Critical)"| NORETRY
    CHECK1 -->|"No — auto-detect<br/>from HTTP method"| CHECK2
    CHECK2 -->|"GET"| READ
    CHECK2 -->|"HEAD / OPTIONS"| HEALTH
    CHECK2 -->|"PUT"| CRITICAL
    CHECK2 -->|"POST / DELETE / PATCH"| WRITE

    classDef decisionStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef pipeStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9

    class CHECK1,CHECK2 decisionStyle
    class READ,HEALTH,CRITICAL,WRITE,NORETRY pipeStyle
```

> [!NOTE]
> This is implemented in [ResiliencePipelineSelector.Resolve()](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Policies/HttpRequestPipelineOptions.cs#L10-L27). Service clients can override the auto-detection by explicitly passing a `pipeline:` parameter, as seen in [PaymentServiceClient](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Clients/Payment/PaymentServiceClient.cs#L30) using `ResiliencePipelineKeys.NoRetry` for payment creation (because payments are non-idempotent and must never be retried).

---

## 17. The 5 Resilience Pipelines — Configuration & Defaults

Each pipeline is a **Polly `Policy.WrapAsync`** composing up to 4 layers. The [HttpClientResiliencePolicyFactory](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Policies/HttpClientResiliencePolicyFactory.cs) builds them on first use and caches them per `{clientName}:{pipelineKey}`.

### Pipeline Composition (Execution Order: Outside → Inside)

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart LR

    subgraph POLLY_WRAP["🛡️ Policy.WrapAsync — Execution Order"]
        direction LR
        BH["1️⃣ <b>Bulkhead</b><br/>Concurrency Gate<br/>━━━━━━━━━━━━━━━<br/>Limits parallel requests<br/>Queues overflow<br/>Rejects if queue full"]
        CB["2️⃣ <b>Circuit Breaker</b><br/>Fail-Fast Switch<br/>━━━━━━━━━━━━━━━<br/>Opens after N failures<br/>Half-open probe<br/>Auto-reset on success"]
        RT["3️⃣ <b>Retry</b><br/>Exponential Backoff<br/>━━━━━━━━━━━━━━━<br/>Retries on transient errors<br/>429 / 5xx / Timeout<br/>delay × 2^(attempt-1)"]
        TO["4️⃣ <b>Timeout</b><br/>Per-Attempt Guard<br/>━━━━━━━━━━━━━━━<br/>Cancels request if<br/>individual attempt<br/>exceeds limit"]
    end

    BH --> CB --> RT --> TO

    classDef bhStyle fill:#b45309,stroke:#fbbf24,stroke-width:2px,color:#fef9c3
    classDef cbStyle fill:#be123c,stroke:#fb7185,stroke-width:2px,color:#fff1f2
    classDef rtStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef toStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe

    class BH bhStyle
    class CB cbStyle
    class RT rtStyle
    class TO toStyle

    style POLLY_WRAP fill:#0f172a,stroke:#7c3aed,stroke-width:2px,color:#c4b5fd
```

### Default Settings per Pipeline

All defaults are defined in [DownstreamHttpClientOptions.cs](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Configuration/DownstreamHttpClientOptions.cs#L33-L89) and can be overridden per-client in `appsettings.json`:

| Pipeline | Timeout | Retry Attempts | Retry Delay | Circuit Breaker | CB Failures | CB Break Duration | Use Case |
|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| **Read** | 10s | 3 | 200ms × 2^n | ✅ Enabled | 5 | 30s | `GET` — Safe to retry, read-only |
| **Write** | 12s | 0 | — | ✅ Enabled | 5 | 30s | `POST`/`DELETE` — Non-idempotent by default, no retries |
| **Health** | 2s | 1 | 100ms | ❌ Disabled | — | — | `HEAD`/`OPTIONS` — Fast probe, minimal overhead |
| **Critical** | 15s | 2 | 300ms × 2^n | ✅ Enabled | 3 | 60s | `PUT` — Idempotent status updates, aggressive CB |
| **NoRetry** | 10s | 0 | — | ✅ Enabled | 5 | 30s | Payments — Explicitly no retries, CB only |

### What Triggers a Retry or Circuit Break?

The factory handles these conditions (defined in [HttpClientResiliencePolicyFactory.cs](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Policies/HttpClientResiliencePolicyFactory.cs#L81-L101)):

```
HttpPolicyExtensions.HandleTransientHttpError()   // 5xx + 408 (Request Timeout)
    .Or<TimeoutRejectedException>()                // Polly inner timeout fired
    .Or<TaskCanceledException>()                   // .NET cancellation / HTTP timeout
    .OrResult(r => r.StatusCode == 429)            // Rate limited (Too Many Requests)
```

---

## 18. The Request Handler Pipeline — Layer by Layer

### Layer 1: HeaderPropagationHandler

[HeaderPropagationHandler.cs](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Handlers/HeaderPropagationHandler.cs) is a `DelegatingHandler` that runs before every outbound request:

| Action | Detail |
|---|---|
| **Propagate from incoming request** | `Authorization`, `X-Correlation-Id`, `X-Country` — carries the original user's context across service boundaries |
| **Add caller identity** | `X-App-Id` header (e.g. `order-service`) — identifies which service is calling |
| **HMAC signature** (optional) | Computes `HMACSHA256(AppId + Method + Path + Timestamp)` using a shared secret, adds `X-App-Signature` and `X-App-Timestamp` headers |

> [!TIP]
> **Why HMAC signatures?** They prevent unauthorized internal services from impersonating the calling service. The downstream can verify the signature matches the expected shared secret, ensuring the request genuinely came from `order-service`.

### Layer 2: Polly Policy Handler

The `.AddPolicyHandler()` middleware inspects each request, resolves the correct pipeline key, and applies the cached Polly `IAsyncPolicy<HttpResponseMessage>` wrapping Bulkhead → Circuit Breaker → Retry → Timeout.

### Layer 3: SocketsHttpHandler

Configured in [OutboundHttpServiceCollectionExtensions.cs](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Extensions/OutboundHttpServiceCollectionExtensions.cs#L61-L67):

| Setting | Default | Purpose |
|---|:---:|---|
| `MaxConnectionsPerServer` | 64 | TCP connection pool limit per downstream host |
| `PooledConnectionLifetime` | 300s | Rotates pooled connections to pick up DNS changes |
| `PooledConnectionIdleTimeout` | 120s | Closes idle sockets to free system resources |
| `ConnectTimeout` | 10s | TCP connection establishment timeout |

---

## 19. Service Clients — Real Usage in the Codebase

### Which Service Calls Which?

```mermaid
%%{init: {"theme": "dark"}}%%

flowchart LR

    subgraph CLIENTS["📡 Typed HTTP Service Clients"]
        direction TB
        OS["📦 Order Service"]
        PS["💳 Payment Service"]
    end

    PSC["PaymentServiceClient<br/>pipeline: <b>NoRetry</b>"]
    PRSC["ProductServiceClient<br/>pipeline: <b>Write</b>"]
    OSC["OrderServiceClient<br/>pipeline: <b>Critical</b>"]

    OS -->|"CreatePaymentAsync"| PSC
    OS -->|"DecreaseStockAsync<br/>IncreaseStockAsync<br/>DecreaseStockBulkAsync<br/>IncreaseStockBulkAsync"| PRSC
    PS -->|"UpdateOrderStatusAsync"| OSC

    PSC -->|"POST /api/v1/payments<br/>+ X-Idempotency-Key"| PS_TARGET["💳 Payment Service"]
    PRSC -->|"POST /api/v1/products/{id}/*<br/>+ X-Idempotency-Key"| PRS_TARGET["🏷️ Product Service"]
    OSC -->|"PUT /api/v1/orders/{id}/status"| OS_TARGET["📦 Order Service"]

    classDef svcStyle fill:#0d9488,stroke:#2dd4bf,stroke-width:2px,color:#f1f5f9
    classDef clientStyle fill:#6d28d9,stroke:#a78bfa,stroke-width:2px,color:#ede9fe
    classDef targetStyle fill:#1d4ed8,stroke:#60a5fa,stroke-width:2px,color:#dbeafe

    class OS,PS svcStyle
    class PSC,PRSC,OSC clientStyle
    class PS_TARGET,PRS_TARGET,OS_TARGET targetStyle
```

### Why Each Client Uses Its Specific Pipeline

| Client | Pipeline | Rationale |
|---|---|---|
| [PaymentServiceClient](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Clients/Payment/PaymentServiceClient.cs) | **NoRetry** | Payment creation is **non-idempotent** — retrying could double-charge. Uses `X-Idempotency-Key` for server-side dedup instead |
| [ProductServiceClient](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Clients/Product/ProductServiceClient.cs) | **Write** | Stock operations are protected by `X-Idempotency-Key`, but retries are disabled by default in the Write pipeline. The key ensures safe replay at the server level |
| [OrderServiceClient](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Clients/Order/OrderServiceClient.cs) | **Critical** | Order status updates (`PUT`) are idempotent — safe to retry aggressively. Uses the Critical pipeline with 2 retries and a strict 60s circuit break |

---

## 20. How to Add a New Service Client — Step-by-Step

> [!IMPORTANT]
> Follow these 5 steps to add a new typed HTTP client for calling a new downstream microservice.

### Step 1: Create the Interface

Create `Micro.Shared/Http/Clients/Catalog/ICatalogServiceClient.cs`:

```csharp
namespace Micro.Shared.Http.Clients.Catalog;

public interface ICatalogServiceClient
{
    Task<ApiResult<CatalogItemDto>> GetItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);
    
    Task<ApiResult<object>> CreateItemAsync(
        CreateCatalogItemRequest request,
        CancellationToken cancellationToken = default);
}
```

### Step 2: Create the Implementation

Create `Micro.Shared/Http/Clients/Catalog/CatalogServiceClient.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;
using Micro.Shared.Http.Clients.Common;

namespace Micro.Shared.Http.Clients.Catalog;

public sealed class CatalogServiceClient : DownstreamApiClientBase, ICatalogServiceClient
{
    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<CatalogItemDto>> GetItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        // GET → automatically uses the "Read" pipeline (3 retries, 10s timeout)
        return GetAsync<CatalogItemDto>(
            endpoint: $"api/v1/catalog/{itemId}",
            pipeline: ResiliencePipelineKeys.Read,
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<object>> CreateItemAsync(
        CreateCatalogItemRequest request,
        CancellationToken cancellationToken = default)
    {
        // POST with idempotency key → safe to retry
        return PostAsync<CreateCatalogItemRequest, object>(
            endpoint: "api/v1/catalog",
            request: request,
            pipeline: ResiliencePipelineKeys.Write,
            useIdempotencyKey: true,
            cancellationToken: cancellationToken);
    }
}
```

### Step 3: Register the DI Extension

Add to [OutboundHttpServiceCollectionExtensions.cs](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/Micro.Shared/Http/Extensions/OutboundHttpServiceCollectionExtensions.cs):

```csharp
public static IServiceCollection AddCatalogServiceClient(
    this IServiceCollection services, IConfiguration configuration)
{
    return services.AddDownstreamClient<ICatalogServiceClient, CatalogServiceClient>(
        configuration,
        "CatalogService",              // Client name (used for config + policy cache key)
        "Services:CatalogService");     // Fallback base URL config key
}
```

### Step 4: Configure in `appsettings.json`

Add the `CatalogService` config to the consuming service's `appsettings.json`:

```json
{
  "Services": {
    "CatalogService": "http://catalog-service:8080"
  },
  "OutboundHttp": {
    "Clients": {
      "CatalogService": {
        "BaseUrl": "http://catalog-service:8080",
        "Pipelines": {
          "Read": {
            "TimeoutSeconds": 5,
            "RetryAttempts": 2,
            "EnableRetry": true,
            "EnableCircuitBreaker": true
          }
        }
      }
    }
  }
}
```

### Step 5: Register in `Program.cs`

In the consuming microservice's `Program.cs`:

```csharp
builder.Services.AddOutboundHttpInfrastructure();    // Required once
builder.Services.AddCatalogServiceClient(builder.Configuration);
```

### Configuration Hierarchy

Settings are resolved in this priority (later overrides earlier):

```
1. DownstreamHttpClientOptions defaults (code)
        ↓ overridden by
2. OutboundHttp:Defaults (appsettings.json)
        ↓ overridden by
3. OutboundHttp:Clients:{ClientName} (appsettings.json)
        ↓ overridden by
4. OutboundHttp:Clients:{ClientName}:Pipelines:{PipelineKey} (appsettings.json)
```

---
---

# 🐘 PostgreSQL Infrastructure Deep Dive

> This section provides a detailed breakdown of the PostgreSQL infrastructure scripts in the [infra/postgres](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres) directory.

---

## 21. Script Overview & Execution

These scripts automate the setup of a **primary (write-db)** and a **physical standby (read-db)** PostgreSQL cluster using Docker.

### [write-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/write-startup.sh)

This script acts as the entrypoint wrapper for the **primary** database container.

| Attribute | Detail |
|---|---|
| **Execution** | Runs every time the primary container starts |
| **PID 1** | Starts PostgreSQL in the background, runs setup, then `wait $PG_PID` to keep container alive |

**What it does (in order):**
1. Starts PostgreSQL in the background and waits for it to be ready
2. Configures `pg_hba.conf` for replication and `scram-sha-256` password authentication
3. Creates a dedicated replication user (`replicator`) with `LOGIN REPLICATION` privileges
4. Creates required databases (`OrderDb`, `PaymentDb`, `ProductDb` + USA variants) idempotently
5. Creates logical replication publications idempotently
6. Brings the Postgres process to the foreground via `wait $PG_PID`

> [!NOTE]
> Automatic schema migrations have been removed from this script. Migrations should be applied manually against the primary database after it has started. Because physical replication streams all disk changes, any migration applied to the primary is **instantly and automatically replicated** to all standbys.

### [read-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/read-startup.sh)

This script acts as the entrypoint wrapper for the **replica** database container.

| Attribute | Detail |
|---|---|
| **Execution** | Runs every time the replica container starts |
| **Key Decision** | Checks if `$PGDATA/PG_VERSION` exists — if empty, runs `pg_basebackup`; if exists, skips |

**What it does:**
1. Checks if the data directory (`$PGDATA`) is uninitialized
2. If uninitialized → waits for primary → executes `pg_basebackup` with `--create-slot`
3. Starts PostgreSQL in read-only standby mode via `exec docker-entrypoint.sh postgres`

---

## 22. How to Add a New Database

To introduce a new database (e.g., `CatalogDb`), update the idempotent initialization logic in [write-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/write-startup.sh):

**Add to the `EOSQL` block:**
```sql
SELECT 'CREATE DATABASE "CatalogDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'CatalogDb')\gexec
SELECT 'CREATE DATABASE "CatalogDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'CatalogDb-USA')\gexec
```

**Add a publication:**
```bash
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "CatalogDb" -c \
  "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'catalog_pub') THEN CREATE PUBLICATION catalog_pub FOR ALL TABLES; END IF; END \$\$;"
```

**Add PgBouncer routing** in [pgbouncer.ini](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/pgbouncer/pgbouncer.ini):
```ini
CatalogDb = host=write-db port=5432 dbname=CatalogDb
CatalogDbReplica = host=read-db port=5432 dbname=CatalogDb
CatalogDb-USA = host=write-db port=5432 dbname=CatalogDb-USA
CatalogDb-USAReplica = host=read-db port=5432 dbname=CatalogDb-USA
```

---

## 23. The Replication User — Roles vs. Users vs. Privileges

In PostgreSQL, the concepts of "users" and "roles" are effectively the same thing:

| Concept | Definition |
|---|---|
| **Role** | An entity that can own database objects and have database privileges |
| **User** | Simply a Role that has been granted the `LOGIN` privilege |
| **`REPLICATION`** | A special privilege attribute that can be granted to any role |

In [write-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/write-startup.sh#L27), we create a role with both `LOGIN` and `REPLICATION`:

```sql
CREATE ROLE replicator LOGIN REPLICATION PASSWORD 'change_me';
```

Because it has `LOGIN`, it acts as a "user". When the replica starts, it authenticates to the primary using this `replicator` username and password — exactly like a normal user would.

### Can You Have Multiple Replication Users?

**Yes.** You can create as many users as you want with the `REPLICATION` attribute.

| Scenario | Benefit |
|---|---|
| `replica_1_user`, `replica_2_user` | Audit connections per replica |
| Separate credentials per standby | Revoke access to one replica without affecting others |
| Password rotation | Rotate passwords independently per standby |

For a standard setup, a single shared user (like `replicator`) used by all standbys is perfectly fine.

---

## 24. Physical Replication Deep Dive & Replication Slots

### What Is Physical Replication?

Physical replication (Streaming Replication) creates a **bit-for-bit, exact copy** of the primary database cluster. It streams the physical **Write-Ahead Logs (WAL)** from the primary to the standby. The standby continuously applies these logs at the disk block level.

```mermaid
%%{init: {"theme": "dark"}}%%

sequenceDiagram
    autonumber
    participant R as 🟢 Replica (read-db)
    participant P as 🔵 Primary (write-db)
    participant D as 💾 WAL Files on Primary

    rect rgb(30, 58, 138)
        Note over R,P: Phase 1: Initialization (Base Backup)
        R->>P: Connect as 'replicator' user
        R->>P: CREATE_REPLICATION_SLOT 'replica_1_slot'
        P->>D: Mark starting WAL position in slot
        P->>R: Stream entire data directory (pg_basebackup)
        R->>R: Write data to $PGDATA
    end

    rect rgb(4, 120, 87)
        Note over R,P: Phase 2: Streaming WAL (Continuous)
        R->>P: "I am replica_1_slot — send WAL from my last position"
        loop Continuous Streaming
            P->>D: Write new WAL segment
            D->>R: Stream WAL over TCP
            R->>R: Apply WAL (replay at disk block level)
            R->>P: Acknowledge WAL receipt
        end
    end

    rect rgb(91, 33, 182)
        Note over R,P: Disconnection & Reconnection
        R--xP: Replica disconnects (network issue)
        Note over P,D: 🔖 Slot prevents WAL deletion!<br/>Primary keeps WAL files until replica catches up
        R->>P: Replica reconnects
        R->>P: "Send WAL from my last acknowledged position"
        D->>R: Stream buffered WAL segments
        R->>R: Catch up to current state
    end
```

### The Lifecycle of a Replica & Replication Slots

When a new replica is spun up using [read-startup.sh](file:///c:/Users/Mohamed%20Essam/Desktop/MicroService/MicroservicesArchitecture/infra/postgres/read-startup.sh), it executes:

```bash
pg_basebackup -h write-db -p 5432 -U replicator -D "$PGDATA" \
  -Fp -Xs -P -R -c fast --create-slot -S "replica_1_slot"
```

| Flag | Purpose |
|---|---|
| `-Fp` | Plain format (direct file copy) |
| `-Xs` | Stream WAL during backup (not just after) |
| `-P` | Show progress |
| `-R` | Auto-create `standby.signal` and `postgresql.auto.conf` for replication |
| `-c fast` | Fast checkpoint (don't wait for scheduled checkpoint) |
| `--create-slot -S` | Create a named replication slot on the primary |

### Why Use Replication Slots?

> [!CAUTION]
> **Without a slot:** The primary deletes old WAL files to save disk space. If a replica disconnects too long, the primary might delete the WAL files the replica needs — **permanently breaking the replica**.
>
> **With a slot:** The slot acts as a **bookmark** on the primary. It tells the primary: *"Do not delete ANY WAL files until the replica connected to this slot has downloaded them."*

| Aspect | Benefit | Risk |
|---|---|---|
| **Guaranteed catch-up** | A disconnected replica can always catch up, no matter how long it was down | — |
| **WAL retention** | — | If a replica dies permanently and you forget to drop its slot, the primary stores WAL forever → **disk exhaustion** |

**To manually drop an orphaned slot:**
```sql
SELECT pg_drop_replication_slot('replica_1_slot');
```

**To monitor slot lag:**
```sql
SELECT slot_name, active, 
       pg_wal_lsn_diff(pg_current_wal_lsn(), restart_lsn) AS lag_bytes
FROM pg_replication_slots;
```


