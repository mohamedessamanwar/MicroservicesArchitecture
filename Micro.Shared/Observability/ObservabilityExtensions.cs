using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Micro.Shared.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures the complete Observability stack for a microservice.
    /// This method sets up OpenTelemetry (Traces & Metrics) and Serilog (Logs).
    /// It automatically extracts context from incoming HTTP requests and pushes telemetry to the OTel Collector.
    /// 
    /// What it receives: HTTP requests, EF Core queries, RabbitMQ messages (via custom extension), runtime events.
    /// What it produces: W3C Trace context, application logs, and system metrics.
    /// Where data goes: Everything is sent to the OpenTelemetry Collector at 'http://otel-collector:4317'.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder for the microservice.</param>
    /// <param name="serviceName">The unique name of the microservice (e.g., 'order-service') used to identify it in Grafana.</param>
    /// <returns>The modified WebApplicationBuilder.</returns>
    public static WebApplicationBuilder AddApplicationObservability(this WebApplicationBuilder builder, string serviceName)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment.EnvironmentName;

        // Configure Resource Attributes (e.g. Service Name)
        // These attributes are attached to EVERY log, trace, and metric, allowing us to filter by service in Grafana.
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
            .AddTelemetrySdk()
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment,
                ["service.instance.id"] = Environment.MachineName
            });

        // Configure OpenTelemetry Tracing and Metrics
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName: serviceName, serviceVersion: "1.0.0");
                r.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.instance.id"] = Environment.MachineName
                });
            })
            .WithTracing(tracing =>
            {
                tracing
                    // ASP.NET Core Instrumentation: 
                    // Automatically creates a span for every incoming HTTP request.
                    // Extracts W3C traceparent headers to link distributed traces.
                    .AddAspNetCoreInstrumentation() 
                    
                    // HttpClient Instrumentation:
                    // Automatically creates a span for every outgoing HTTP request.
                    // Injects W3C traceparent headers so downstream services can link to this trace.
                    .AddHttpClientInstrumentation() 
                    
                    // EF Core Instrumentation:
                    // Automatically creates a span for every database query.
                    // We set SetDbStatementForText = true to capture the actual SQL query (parameterized safely)
                    // so we can see slow queries in Tempo.
                    .AddEntityFrameworkCoreInstrumentation()
                    
                    // OTLP Exporter:
                    // Sends the generated traces to the OpenTelemetry Collector via gRPC on port 4317.
                    // The Collector then forwards them to Grafana Tempo.
                    .AddOtlpExporter(); 
            })
            .WithMetrics(metrics =>
            {
                metrics
                    // Captures metrics like HTTP request duration, error rates, and request counts.
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    
                    // Captures .NET Runtime metrics (Garbage Collection, Thread Pool, Heap size).
                    .AddRuntimeInstrumentation()
                    
                    // Captures Process metrics (CPU usage, Memory Working Set).
                    .AddProcessInstrumentation()
                    
                    // Standard .NET Kestrel Server metrics.
                    .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
                    
                    // Sends the generated metrics to the OpenTelemetry Collector via gRPC on port 4317.
                    // The Collector then exposes them for Prometheus to scrape.
                    .AddOtlpExporter(); 
            });

        // Configure Serilog with OpenTelemetry Sink
        // We use Serilog to capture structured logs and ensure they contain TraceId and SpanId.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            
            // Enrich with standard properties so we can query them easily in Loki.
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", environment)
            
            // Log to console for local debugging (useful when running without Docker).
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            
            // Log to OpenTelemetry Collector:
            // This sink sends all logs via OTLP (gRPC) to the OpenTelemetry Collector.
            // The Collector then forwards these logs to Grafana Loki.
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["deployment.environment"] = environment
                };
            })
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }
}
