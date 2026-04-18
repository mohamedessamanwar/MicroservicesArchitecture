# HTTP Client Pattern Documentation

This document explains the design pattern used for inter-service communication, specifically the relationship between `BaseApiClient` and service-specific clients like `OrderServiceClient`.

## Pattern Name: **Typed Client Pattern** (with Service Gateway properties)

This is a standard .NET **Typed Client** pattern, enhanced with a base class to act as a **Service Gateway**.

- **Typed Client**: Encapsulates `HttpClient` logic within a strongly-typed class (`OrderServiceClient`), making it easier to use and specific to a domain.
- **Service Gateway**: `BaseApiClient` acts as a gateway that standardizes how all microservices talk to each other (handling logging, errors, serialization uniformly).

## Architecture

### 1. The Base Layer: `BaseApiClient`

File: `Micro.Shared/Http/Clients/BaseApiClient.cs`

This abstract class handles the "plumbing" of HTTP communication so individual clients don't have to repeat it.

**Key Responsibilities:**

- **Standardized Error Handling**: Catches exceptions (Network, Timeout, JSON) and converts them into a safe result object.
- **Response Normalization**: Converts non-200 HTTP responses (4xx, 5xx) into a structured error format.
- **Logging**: Automatically logs all outgoing requests, successes, and failures.
- **Serialization**: Manages `JsonSerializerOptions` centrally (e.g., camelCase policies).

#### Deep Dive: Error Handling Logic

_Ref: Line 46 in `BaseApiClient.cs`_

```csharp
if (!response.IsSuccessStatusCode)
{
    return await HandleErrorResponseAsync<T>(response, method, url);
}
```

**How it works:**

1.  **Detection**: Checks `IsSuccessStatusCode` (is it 200-299?).
2.  **Interception**: If false, it delegates to `HandleErrorResponseAsync`.
3.  **Parsing**: Reads the response body.
4.  **Propagating**: Tries to deserialize the error into an `ApiResult<T>` (presuming the other service also returned a structured error).
5.  **Fallback**: If deserialization works, it returns that structured error. If not, it creates a generic failure result wrapping the raw body.

**Benefit**: The calling code (e.g., logic in your API) doesn't need `try/catch` blocks for HTTP errors. It simply checks `result.Success`.

### 2. The Implementation Layer: `OrderServiceClient`

File: `Micro.Shared/Clients/Order/OrderServiceClient.cs`

This class focuses purely on **Business Logic** and **API Definitions**.

**How it uses the pattern:**

- **Inheritance**: `public class OrderServiceClient : BaseApiClient`
- **Abstraction**: It hides URLs (`api/v1/orders/...`) and HTTP verbs (`GET`, `PUT`) behind clean C# methods.
- **Simplicity**:
  ```csharp
  // Complex HTTP logic is reduced to one line:
  public Task<ApiResult<OrderDto>> GetOrderAsync(Guid id, ...)
      => GetAsync<OrderDto>($"api/v1/orders/{id}", ct);
  ```

## Usage Example

**Registration (Dependency Injection):**

```csharp
// In Program.cs
services.AddHttpClient<IOrderServiceClient, OrderServiceClient>();
```

**Consumption (in a Service):**

```csharp
public class CheckoutService
{
    private readonly IOrderServiceClient _orderClient;

    public async Task ProcessCheckout(Guid orderId)
    {
        // 1. Call the typed client
        var result = await _orderClient.GetOrderAsync(orderId);

        // 2. Check standardized result (thanks to BaseApiClient)
        if (!result.Success)
        {
            // Handle error gracefully
            _logger.LogError("Order failed: {Message}", result.Message);
            return;
        }

        // 3. Access data
        var order = result.Data;
    }
}
```

## Benefits of this Pattern

1.  **DRY (Don't Repeat Yourself)**: Logging, error handling, and serialization are written once in the Base class.
2.  **Resilience**: Centralized place to handle Retries/Circuit Breakers (via the `HttpClient` injected into the Base).
3.  **Maintainability**: If we need to change how we log errors or handle timeouts, we change it in one file (`BaseApiClient.cs`).
4.  **Testability**: `IOrderServiceClient` can be easily mocked in unit tests.
