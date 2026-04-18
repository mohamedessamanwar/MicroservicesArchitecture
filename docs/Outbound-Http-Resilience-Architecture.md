# Outbound HTTP Resilience Architecture

## Purpose
This document describes the production-ready outbound HTTP architecture used between services in this solution.

Design goals:
- Centralized registration for all outbound clients.
- Clear split between client-level transport configuration and endpoint-level resilience behavior.
- Reusable named resilience pipelines (no copy/paste retry logic per method).
- Good observability through structured logs and propagated correlation/tracing headers.

## Architecture Overview

### Client-Level (shared per downstream service)
Configured once in DI registration (typed client registration):
- Base URL.
- `MaxConnectionsPerServer`.
- `PooledConnectionLifetime`.
- `PooledConnectionIdleTimeout`.
- `ConnectTimeout`.
- `HttpClient.Timeout` (outer timeout guard).
- Shared bulkhead/concurrency defaults.
- Header propagation handler (`Authorization`, `X-Correlation-Id`, `X-Country`, `traceparent`, `tracestate`, `baggage`).

Code location:
- `Micro.Shared/Http/Extensions/OutboundHttpServiceCollectionExtensions.cs`
- `Micro.Shared/Http/Handlers/HeaderPropagationHandler.cs`

### Endpoint-Level (operation semantics)
Each outbound request selects a named pipeline key via `HttpRequestMessage.Options`.

Pipeline keys:
- `read`
- `write`
- `health`
- `critical`
- `no-retry`

Selection behavior:
- Explicit per endpoint from typed client methods.
- If no explicit key is set, selector falls back by HTTP method.

Code location:
- `Micro.Shared/Http/Policies/ResiliencePipelineKeys.cs`
- `Micro.Shared/Http/Policies/HttpRequestPipelineOptions.cs`

### Policy Composition
Policies are built per `(clientName, pipelineKey)` and cached.

Composition order:
1. Bulkhead (shared or endpoint override)
2. Circuit breaker (if enabled)
3. Retry (if enabled)
4. Timeout (per attempt)

Code location:
- `Micro.Shared/Http/Policies/HttpClientResiliencePolicyFactory.cs`

## Configuration Model

Options class:
- `DownstreamHttpClientOptions`
- `DownstreamResiliencePipelinesOptions`
- `ResiliencePipelineSettings`

Code location:
- `Micro.Shared/Http/Configuration/DownstreamHttpClientOptions.cs`

### appsettings structure
```json
{
  "OutboundHttp": {
    "Defaults": {
      "MaxConnectionsPerServer": 64,
      "PooledConnectionLifetimeSeconds": 300,
      "PooledConnectionIdleTimeoutSeconds": 120,
      "ConnectTimeoutSeconds": 10,
      "OverallRequestTimeoutSeconds": 100,
      "MaxParallelRequests": 128,
      "MaxQueuedRequests": 256
    },
    "Clients": {
      "PaymentService": {
        "Pipelines": {
          "Read": { "TimeoutSeconds": 8, "RetryAttempts": 2 },
          "NoRetry": { "TimeoutSeconds": 12, "RetryAttempts": 0, "EnableRetry": false },
          "Critical": { "TimeoutSeconds": 15, "RetryAttempts": 2 }
        }
      }
    }
  }
}
```

## Typed Clients

### Payment Service Client
- Interface: `IPaymentServiceClient`
- Implementation: `PaymentServiceClient`
- Endpoint behavior:
  - Create payment uses `no-retry` by default (side-effect write).

Files:
- `Micro.Shared/Clients/Payment/IPaymentServiceClient.cs`
- `Micro.Shared/Clients/Payment/PaymentServiceClient.cs`
- `Micro.Shared/Clients/Payment/DTOs/PaymentDtos.cs`

### Order Service Client
- Interface: `IOrderServiceClient`
- Implementation: `OrderServiceClient`
- Endpoint behavior:
  - Update order status uses `critical` pipeline.

Files:
- `Micro.Shared/Clients/Order/IOrderServiceClient.cs`
- `Micro.Shared/Clients/Order/OrderServiceClient.cs`
- `Micro.Shared/Clients/Order/DTOs/OrderDtos.cs`

## DI Usage in APIs

OrderService API:
```csharp
builder.Services.AddOutboundHttpInfrastructure(builder.Environment.ApplicationName);
builder.Services.AddPaymentServiceClient(builder.Configuration);
```

Payment API:
```csharp
builder.Services.AddOutboundHttpInfrastructure(builder.Environment.ApplicationName);
builder.Services.AddOrderServiceClient(builder.Configuration);
```

## Operational Notes
- Retry is not applied blindly to all operations.
- Side-effect writes default to `no-retry` unless idempotency is guaranteed.
- Timeouts are pipeline-specific.
- Circuit breaker and bulkhead are configurable per pipeline.
- Structured logs include method, path, pipeline, status code, and elapsed time.

## Removed Legacy/Conflicting Components
The following were intentionally removed to avoid dual architecture paths:
- `Micro.Shared/Http/Clients/BaseApiClient.cs`
- `Micro.Shared/Http/Clients/PaymentClient.cs`
- `Micro.Shared/Http/Policies/PolicySelector.cs`
- `Micro.Shared/Http/Policies/PollyPolicies.cs`
- `Micro.Shared/Http/Policies/PolicyKeys.cs`
- `Micro.Shared/Http/Policies/RequestOptionKeys.cs`
- `Micro.Shared/Http/Models/ApiRequest.cs`
- `Micro.Shared/Http/Models/ApiResponse.cs`

## Extending the System
To add a new downstream service:
1. Add client DTOs and interface under `Micro.Shared/Clients/<ServiceName>/`.
2. Implement typed client inheriting `DownstreamApiClientBase`.
3. Add a registration extension method in `OutboundHttpServiceCollectionExtensions`.
4. Add `OutboundHttp:Clients:<ServiceName>` configuration section.

To add a new resilience profile:
1. Add a new key in `ResiliencePipelineKeys`.
2. Add settings to `DownstreamResiliencePipelinesOptions`.
3. Map key in `HttpClientResiliencePolicyFactory.BuildPolicy`.
4. Use that key from typed client endpoint methods.
