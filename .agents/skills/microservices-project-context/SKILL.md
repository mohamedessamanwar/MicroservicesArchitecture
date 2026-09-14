---
name: microservices-project-context
description: >
  Provides full architectural context for the MicroservicesArchitecture project — an E-Commerce 
  platform built with .NET 8, PostgreSQL 16, PgBouncer, Redis 7, RabbitMQ 3, and YARP API Gateway.
  Covers all service boundaries, infrastructure patterns (CQRS, Saga, Outbox/Inbox, Polly resilience),
  multi-tenancy (Egypt/USA), Docker Compose topology, and inter-service HTTP communication.
  Activate this skill whenever modifying, debugging, or extending any part of this microservices system.
---

# MicroservicesArchitecture — Project Context Skill

## Solution Overview

This is an **E-Commerce Microservices Platform** built with **.NET 8**, containerized via **Docker Compose**, 
and deployed as independent business domain services communicating via HTTP and AMQP.

### Solution Structure

```
MicroservicesArchitecture/
├── ApiGateway/                    # YARP reverse proxy (.NET 8)
├── OrderService.Api/              # Order Service API layer
├── OrderService.Application/      # Order CQRS commands/queries
├── OrderService.Domain/           # Order domain entities
├── OrderService.Infrastructure/   # Order EF Core, repositories
├── Payment.Api/                   # Payment Service API layer
├── Payment.Application/           # Payment CQRS commands/queries
├── Payment.Core/                  # Payment domain entities
├── Payment.Infrastructure/        # Payment EF Core, repositories
├── ProductService.Api/            # Product Service API layer
├── ProductService.Application/    # Product CQRS commands/queries
├── ProductService.Domain/         # Product domain entities
├── ProductService.Infrastructure/ # Product EF Core, repositories
├── Micro.Shared/                  # Shared library (HTTP clients, Polly, models)
│   └── Http/
│       ├── Clients/               # Typed HTTP service clients (Order, Payment, Product)
│       ├── Configuration/         # DownstreamHttpClientOptions, CallerIdentity
│       ├── Extensions/            # DI registration (AddPaymentServiceClient, etc.)
│       ├── Handlers/              # HeaderPropagationHandler (DelegatingHandler)
│       ├── Models/                # ApiResult<T>, OutboundHttpRequestOptions
│       └── Policies/              # Polly resilience pipelines (5 pipelines)
├── infra/
│   ├── pgbouncer/                 # PgBouncer config (pgbouncer.ini, Dockerfile)
│   ├── postgres/                  # write-startup.sh, read-startup.sh
│   ├── rabbitmq/                  # init-topology.sh (exchanges, queues, DLX)
│   └── redis/                     # redis.conf
├── docker-compose.yml             # Main compose (9 services)
├── docker-compose.override.yml    # Local dev port mappings
└── MicroservicesArchitecture.sln  # Solution file
```

## Key Architectural Patterns

### 1. CQRS (Command Query Responsibility Segregation)
- **Write path**: Service → PgBouncer (port 6432) → PostgreSQL Primary (write-db)
- **Read path**: Service → PgBouncer → PostgreSQL Read Replica (read-db)
- PgBouncer routes based on database name (e.g., `OrderDb` → write-db, `OrderDbReplica` → read-db)

### 2. Transactional Outbox / Inbox Pattern
- Services save state + insert event into an Outbox table in the **same ACID transaction**
- A background worker polls the Outbox and publishes to RabbitMQ
- The Inbox table deduplicates incoming events by `MessageId`

### 3. Saga Pattern (Choreography)
- **Flow**: OrderCreated → PaymentSucceeded → Order Confirmed + Stock Deducted
- **Compensation**: PaymentFailed → Order Cancelled
- No central orchestrator — services react to events autonomously

### 4. Multi-Tenancy
- Tenant is determined by the `X-Country` header (`Egypt` or `USA`)
- Each tenant has isolated databases: `OrderDb` (Egypt), `OrderDb-USA` (USA)
- Each tenant has isolated RabbitMQ queues: `Egypt.order.Q`, `USA.order.Q`

### 5. Polly Resilience Pipelines (5 named pipelines)
- **Read** (GET): 3 retries, 10s timeout, circuit breaker
- **Write** (POST/DELETE): No retries, 12s timeout, circuit breaker
- **Health** (HEAD/OPTIONS): 1 retry, 2s timeout, no circuit breaker
- **Critical** (PUT): 2 retries, 15s timeout, aggressive circuit breaker
- **NoRetry**: 0 retries, 10s timeout, circuit breaker only

Each pipeline composes: **Bulkhead → Circuit Breaker → Retry → Timeout**

## Infrastructure Components

| Service | Container | Port | Technology |
|---------|-----------|------|------------|
| API Gateway | api-gateway | 5000, 80, 443 | YARP (.NET 8) |
| Order Service | order-service | 8080 | .NET 8 Web API |
| Payment Service | payment-service | 8082 | .NET 8 Web API |
| Product Service | product-service | 8084 | .NET 8 Web API |
| PgBouncer | pgbouncer | 6432 | Connection pooler |
| PostgreSQL Primary | write-db | 5432 | PostgreSQL 16 |
| PostgreSQL Replica | read-db | 5433 | PostgreSQL 16 (Hot Standby) |
| Redis | redis | 6379 | Redis 7 (cache, rate limiter, idempotency) |
| RabbitMQ | rabbitmq | 5672, 15672 | RabbitMQ 3 (AMQP) |

## Inter-Service HTTP Communication

Services communicate via typed `HttpClient`s registered in `Micro.Shared/Http/Extensions/OutboundHttpServiceCollectionExtensions.cs`:

- `AddPaymentServiceClient()` — Order Service → Payment Service (NoRetry pipeline)
- `AddProductServiceClient()` — Order Service → Product Service (Write pipeline)
- `AddOrderServiceClient()` — Payment Service → Order Service (Critical pipeline)

### Adding a New Service Client (Quick Reference)
1. Create `ICatalogServiceClient` interface in `Micro.Shared/Http/Clients/Catalog/`
2. Create `CatalogServiceClient` extending `DownstreamApiClientBase`
3. Add `AddCatalogServiceClient()` method in `OutboundHttpServiceCollectionExtensions.cs`
4. Add `OutboundHttp:Clients:CatalogService` config in consuming service's `appsettings.json`
5. Call `builder.Services.AddCatalogServiceClient(builder.Configuration)` in `Program.cs`

### Header Propagation
The `HeaderPropagationHandler` automatically propagates: `Authorization`, `X-Correlation-Id`, `X-Country`, 
and adds `X-App-Id` + optional `X-App-Signature` (HMAC-SHA256) for service-to-service authentication.

## Database Credentials (Local Dev)

| Component | Host | Port | User | Password |
|-----------|------|------|------|----------|
| PgBouncer | localhost | 6432 | admin | pass |
| PostgreSQL Primary | localhost | 5432 | admin | pass |
| PostgreSQL Replica | localhost | 5433 | admin | pass |
| Redis | localhost | 6379 | — | — |
| RabbitMQ | localhost | 5672/15672 | admin | admin123 |

## API Routes via Gateway

| Route | Service |
|-------|---------|
| `/api/v1/Orders/**` | order-service |
| `/api/v1/Payments/**` | payment-service |
| `/api/v1/Products/**` | product-service |

## Key Files to Know

- `docker-compose.yml` — All 9 services defined
- `infra/postgres/write-startup.sh` — Primary DB initialization (idempotent)
- `infra/postgres/read-startup.sh` — Replica initialization (pg_basebackup + replication slot)
- `infra/rabbitmq/init-topology.sh` — Exchange/Queue/DLX setup per tenant
- `infra/pgbouncer/pgbouncer.ini` — Database routing (12 logical databases)
- `Micro.Shared/Http/Policies/HttpClientResiliencePolicyFactory.cs` — Polly pipeline builder
- `Micro.Shared/Http/Policies/ResiliencePipelineKeys.cs` — Pipeline key constants
- `Micro.Shared/Http/Extensions/OutboundHttpServiceCollectionExtensions.cs` — Client DI registration
- `architecture_decisions.md` — Rate limiting & protection architecture decisions
