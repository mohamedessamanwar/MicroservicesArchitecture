# Docker Architecture & Dependencies

This document provides a comprehensive overview of the Docker Compose dependencies (who runs before who) and the exposed localhost ports for both the existing infrastructure and the new Observability stack.

## Dependency Graph (Who runs before who)

The containers start in a specific order enforced by `depends_on` conditions (`service_healthy` or `service_completed_successfully`).

```mermaid
flowchart TD
    %% Core Infrastructure
    WriteDB[write-db (PostgreSQL)]
    ReadDB[read-db (PostgreSQL Replica)]
    PgBouncer[pgbouncer]
    Redis[redis]
    RabbitMQ[rabbitmq]
    RabbitInit[rabbitmq-init]
    
    %% Observability Stack
    Loki[loki]
    Prometheus[prometheus]
    Tempo[tempo]
    OTelCollector[otel-collector]
    Grafana[grafana]

    %% Microservices
    OrderService[order-service]
    PaymentService[payment-service]
    ProductService[product-service]
    ApiGateway[api-gateway]

    %% Dependencies
    ReadDB -->|depends on| WriteDB
    PgBouncer -->|depends on| WriteDB
    PgBouncer -->|depends on| ReadDB
    RabbitInit -->|depends on| RabbitMQ

    OTelCollector -->|depends on| Loki
    OTelCollector -->|depends on| Tempo
    Grafana -->|depends on| Prometheus
    Grafana -->|depends on| Loki
    Grafana -->|depends on| Tempo

    OrderService -->|depends on| PgBouncer
    OrderService -->|depends on| RabbitInit
    OrderService -->|depends on| Redis
    OrderService -->|depends on| OTelCollector

    PaymentService -->|depends on| PgBouncer
    PaymentService -->|depends on| RabbitInit
    PaymentService -->|depends on| Redis
    PaymentService -->|depends on| OTelCollector

    ProductService -->|depends on| PgBouncer
    ProductService -->|depends on| RabbitInit
    ProductService -->|depends on| Redis
    ProductService -->|depends on| OTelCollector

    ApiGateway -->|depends on| OrderService
    ApiGateway -->|depends on| PaymentService
    ApiGateway -->|depends on| ProductService
    ApiGateway -->|depends on| OTelCollector
```

### Dependency Chain Summary
1. **Base Infrastructure**: `write-db`, `redis`, and `rabbitmq` start first.
2. **Dependent Infrastructure**: `read-db` waits for `write-db`. `pgbouncer` waits for both DBs. `rabbitmq-init` waits for `rabbitmq` to be healthy before running topology scripts.
3. **Observability Core**: `loki`, `tempo`, and `prometheus` start. Then `otel-collector` waits for `loki` and `tempo` to be healthy.
4. **Microservices**: `order-service`, `payment-service`, and `product-service` start after `pgbouncer`, `rabbitmq-init`, `redis`, and `otel-collector` are healthy.
5. **Gateway & UI**: `api-gateway` waits for all microservices. `grafana` waits for `prometheus`, `loki`, and `tempo`.

---

## Localhost URLs and Ports

You can connect to the following services on your host machine (e.g., using a browser, Postman, or IDE tools like DataGrip).

### Infrastructure & Databases
| Service | Localhost Port | Internal Port | Description | Credentials / Connection String |
| :--- | :--- | :--- | :--- | :--- |
| **PostgreSQL Primary** | `5432` | 5432 | Direct write DB access | `User: admin`, `Pass: pass` |
| **PostgreSQL Replica** | `5433` | 5432 | Direct read DB access | `User: admin`, `Pass: pass` |
| **PgBouncer** | `6432` | 6432 | DB connection pooler | `User: admin`, `Pass: pass` |
| **Redis** | `6380` | 6379 | Distributed cache / Rate limits | *No auth* |
| **RabbitMQ TCP** | `5672` | 5672 | AMQP connection | `User: admin`, `Pass: admin123` |
| **RabbitMQ UI** | `15672` | 15672 | Management Interface | [http://localhost:15672](http://localhost:15672) |

### Microservices & API Gateway
*(Note: Direct access to microservices relies on `ASPNETCORE_HTTP_PORTS` in `docker-compose.override.yml`, but they primarily expose internal ports. The API Gateway is the public entry point).*

| Service | Localhost Port | Internal Port | URL |
| :--- | :--- | :--- | :--- |
| **API Gateway (HTTPS)** | `443` | 8081 | [https://localhost:443](https://localhost:443) |
| **API Gateway (HTTP)** | *(internal)* | 8080 | [http://api-gateway:8080](http://api-gateway:8080) |
| **Order Service** | *(internal)* | 8080 | [http://order-service:8080](http://order-service:8080) |
| **Payment Service** | *(internal)* | 8080 | [http://payment-service:8080](http://payment-service:8080) |
| **Product Service** | *(internal)* | 8080 | [http://product-service:8080](http://product-service:8080) |

### Observability Stack
| Service | Localhost Port | Description | URL |
| :--- | :--- | :--- | :--- |
| **Grafana** | `3000` | Unified Dashboards | [http://localhost:3000](http://localhost:3000) (`admin`/`admin`) |
| **Prometheus UI** | `9090` | Metrics query interface | [http://localhost:9090](http://localhost:9090) |
| **Loki** | `3100` | Log ingestion/query | *(Used internally by Grafana)* |
| **Tempo** | `3200` | Traces query/ingestion | *(Used internally by Grafana)* |
| **OTel Collector** | `4317` (gRPC), `4318` (HTTP) | Telemetry ingestion | *(Used internally by Microservices)* |
