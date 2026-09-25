using ApiGateway.Middleware;
using OpenTelemetry.Metrics;
using Micro.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationObservability("api-gateway");

// Configure Kestrel limits
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    // Setting MaxRequestBodySize if needed, keeping default (approx 30MB) or as requested:
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Health Checks
builder.Services.AddHealthChecks();

// Add OpenTelemetry Metrics for YARP
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Yarp.ReverseProxy");
        metrics.AddPrometheusExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GatewayLoggingMiddleware>();

app.UseRouting();

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Map Health Check endpoint
app.MapHealthChecks("/health");

// Map YARP routes
app.MapReverseProxy();

await app.RunAsync();
