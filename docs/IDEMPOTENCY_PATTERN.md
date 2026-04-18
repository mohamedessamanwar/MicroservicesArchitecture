# Idempotency Pattern Implementation Guide

This document describes the implementation of the Idempotency pattern in our microservices architecture, specifically applied to the Payment Service.

## 1. What is Idempotency?

Idempotency is the property of certain operations in mathematics and computer science whereby they can be applied multiple times without changing the result beyond the initial application.

In the context of APIs, an idempotent endpoint ensures that if a client sends the same request multiple times (e.g., due to network retries), the side effects happen only once, and the client receives the same response as the first successful call.

## 2. Implementation Overview

We have implemented a shared idempotency mechanism in the `Micro.Shared` library using:

- **Redis**: To store request results and processing states.
- **Action Filters**: To intercept requests and handle cached responses.
- **Custom Header**: `X-Idempotency-Key` is used to identify unique requests.

### Core Components

1.  **`[Idempotent]` Attribute**: Used to mark controller actions that require idempotency.
2.  **`IdempotencyFilter`**: Logic that checks Redis for existing keys, handles "Processing" states (to prevent race conditions), and caches successful responses.
3.  **`IdempotencyService`**: Helper service to interact with `IRedisRepository`.

## 3. How to use Idempotency

### Step 1: Register Services

In your `Program.cs`, ensure Redis and Idempotency services are registered:

```csharp
using Micro.Shared.Caching;
using Micro.Shared.Http.Idempotency;

// ...
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddIdempotency();
```

### Step 2: Apply the Attribute

Apply the `[Idempotent]` attribute to your POST or PUT actions:

```csharp
[HttpPost]
[Idempotent(ExpirationHours = 24)]
public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
{
    // Your logic here
}
```

### Step 3: Client Usage

Clients **MUST** include the `X-Idempotency-Key` header with a unique value (usually a UUID/GUID) for each unique transaction.

**Example Header:**
`X-Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000`

## 4. Workflow Details

1.  **Request Arrival**: Filter checks for `X-Idempotency-Key`. If missing, returns `400 BadRequest`.
2.  **Cache Check**: Filter checks Redis for `idempotency:{ActionName}:{Key}`.
3.  **Handling States**:
    - **Found & Finished**: Returns the cached response immediately.
    - **Found & Processing**: Returns `409 Conflict` (tells client the request is still in progress).
    - **Not Found**:
      - Marks the key as "Processing" in Redis with a TTL.
      - Proceeds to execute the action.
      - Once finished, updates Redis with the result and marks it as "Finished".
4.  **Error Handling**: If the action throws an exception, the "Processing" flag is removed from Redis, allowing the client to retry the request.

## 5. Storage

Idempotency data is stored in Redis. The default expiration is 24 hours, but it can be customized via the attribute property `ExpirationHours`.

## 6. Benefits

- **Safety**: Prevents duplicate payments or double-processing of orders.
- **Reliability**: Safe retries for clients in case of network failures.
- **Consistency**: Ensures the system remains in a consistent state even with unreliable networks.

## 7. Resilience & Idempotency: Better Together

When you combine **Polly Resilience Policies** (Retries, Timeouts) with the **Idempotency Pattern**, you create a "bulletproof" communication channel between microservices.

### Scenario A: The "Ghost" Request (Network Drop on Response)

1.  **Request 1**: `OrderService` calls `PaymentService` to charge $100.
2.  **Processing**: `PaymentService` successfully charges the card and saves to DB.
3.  **Failure**: The network cable is cut _before_ the response gets back to `OrderService`.
4.  **Resilience**: `OrderService` hits a timeout and Polly executes a **Retry**.
5.  **Idempotency**: `PaymentService` receives the retry (same key). It sees the payment is already done. It returns `201 Created` with the _original_ transaction ID.
6.  **Result**: The user is charged **exactly once**, and both services are in sync.

### Scenario B: The "Impatient" Client (Timeout)

1.  **Request 1**: `OrderService` calls `PaymentService`.
2.  **Lag**: `PaymentService` is slow due to high load.
3.  **Resilience**: `OrderService` timeout triggers (e.g., after 2 seconds). Polly sends **Retry 2**.
4.  **Idempotency**: `PaymentService` receives **Retry 2** while **Request 1** is _still processing_.
5.  **Protection**: The Idempotency Filter returns `409 Conflict`.
6.  **Result**: Prevents duplicate active threads and ensures only the first request completes the business logic.

### Scenario C: Transient Server Error

1.  **Request 1**: `PaymentService` receives a request but its Database is momentarily unavailable.
2.  **Cleanup**: The `IdempotencyFilter` catches the exception and **deletes the processing key** from Redis.
3.  **Resilience**: Polly waits 1 second and executes **Retry 2**.
4.  **Recovery**: Database is back up. `PaymentService` treats this as a fresh request (no key in Redis).
5.  **Result**: The request succeeds on the second try without being "stuck" due to the previous failure.

### Scenario D: The "Fail-Fast" (Circuit Breaker)

1.  **Failure**: `PaymentService` is down (10 consecutive failures).
2.  **Resilience**: Polly's **Circuit Breaker** opens.
3.  **Avoidance**: Subsequent calls from `OrderService` fail immediately without even hitting the network.
4.  **Result**: Saves system resources and prevents "filling up" the Idempotency cache with useless data.
