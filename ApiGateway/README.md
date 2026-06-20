# ApiGateway

This project is a YARP-based API Gateway for the `Order` and `Payment` microservices.

## Running Locally

To run the gateway locally via Docker Compose, simply run:

```bash
docker-compose up
```

Alternatively, you can run it directly using `dotnet run` from the `ApiGateway` directory. If you run it locally (outside of Docker), it will look for the backend services on `http://localhost:8080` (Order) and `http://localhost:8082` (Payment) as configured in `appsettings.Development.json`.

## Base URL

When running via Docker Compose (or `docker-compose.override.yml`), the API Gateway is exposed at:

**`http://localhost:5000`**

## Route Mappings

The gateway routes requests to the backend services based on the following rules:

- **Orders**: `http://localhost:5000/api/v1/Orders/{**catch-all}` ➔ routes to `Order Service`.
- **Payments**: `http://localhost:5000/api/v1/Payments/{**catch-all}` ➔ routes to `Payment Service`.

## Required Headers

The Gateway automatically forwards all headers sent by the client. It also explicitly ensures that the following header is present:
- `X-Correlation-ID`: If a client does not provide this header, the gateway will generate a new UUID and forward it to the backend. This header is also injected into the API gateway logs.

If your services require other headers (like `X-Country`, `Authorization`, `Accept-Language`, etc.), you can safely pass them through the Gateway, and YARP will automatically forward them.

## Changing Backend Destinations

To change the backend addresses, modify the `Destinations` block inside `appsettings.json` (for Docker/production) or `appsettings.Development.json` (for local runs without Docker):

```json
"Destinations": {
  "orders-service": {
    "Address": "http://order-service:8080/"
  }
}
```

## YARP Connection Settings

The gateway uses the following HTTP limits configured in `appsettings.json`:
- `MaxConnectionsPerServer`: 100
- `ActivityTimeout`: 30 seconds

These limits apply to outbound connections from the gateway to the backend clusters.

## Logging

Structured logging is provided by standard ASP.NET Core ILogger. The custom `GatewayLoggingMiddleware` logs metadata for each request, including:
- Request Correlation ID
- Method, Path, Status Code
- Duration in ms
- YARP Route and Cluster IDs

No sensitive data (like the Request/Response bodies or `Authorization` headers) is logged by this middleware.

## Observability

- **Metrics**: OpenTelemetry is configured to expose Prometheus-compatible metrics at `/metrics`. This includes connection stats, request duration, request rates, etc.
- **Health**: A basic health check endpoint is available at `/health`. Note: Currently, this only checks the health of the Gateway itself, as the backends do not expose `/health` endpoints.

## Swagger

Swagger/OpenAPI UI remains available at the individual service level (e.g., `http://localhost:8080/swagger` for Order). The API gateway does not aggregate Swagger definitions to avoid complexity and tight coupling.
