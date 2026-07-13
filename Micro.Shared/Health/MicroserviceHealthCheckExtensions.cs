using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
using System.Net.Sockets;

namespace Micro.Shared.Health;

public static class MicroserviceHealthCheckExtensions
{
    public static IServiceCollection AddMicroserviceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck<MicroserviceReadinessHealthCheck>("readiness", tags: new[] { "ready" });

        return services;
    }

    public static IEndpointRouteBuilder MapMicroserviceHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    check = "live"
                });
            }
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    entries = report.Entries.Select(e => new
                    {
                        key = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                });
            }
        });

        return endpoints;
    }
}

public sealed class MicroserviceReadinessHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MicroserviceReadinessHealthCheck> _logger;

    public MicroserviceReadinessHealthCheck(IConfiguration configuration, ILogger<MicroserviceReadinessHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // 1. Verify Primary DB (Egypt Primary as canonical primary connection check)
        var primaryConnStr = _configuration["ConnectionStrings:Egypt:Primary"];
        if (!string.IsNullOrWhiteSpace(primaryConnStr))
        {
            try
            {
                await using var conn = new NpgsqlConnection(primaryConnStr);
                await conn.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"Primary DB connection failed: {ex.Message}");
            }
        }

        // 2. Verify Replica DB
        var replicaConnStr = _configuration["ConnectionStrings:Egypt:Replica"];
        if (!string.IsNullOrWhiteSpace(replicaConnStr))
        {
            try
            {
                await using var conn = new NpgsqlConnection(replicaConnStr);
                await conn.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"Replica DB connection failed: {ex.Message}");
            }
        }

        // 3. Verify Redis
        var redisConnStr = _configuration["ConnectionStrings:Redis"];
        if (!string.IsNullOrWhiteSpace(redisConnStr))
        {
            try
            {
                var hostPort = redisConnStr.Split(',')[0];
                var parts = hostPort.Split(':');
                var host = parts[0];
                var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 6379;

                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port, cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"Redis TCP check failed: {ex.Message}");
            }
        }

        // 4. Verify RabbitMQ
        var rabbitHost = _configuration["RabbitMq:Host"] ?? "rabbitmq";
        var rabbitPort = _configuration.GetValue<int>("RabbitMq:Port", 5672);
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(rabbitHost, rabbitPort, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add($"RabbitMQ TCP check failed: {ex.Message}");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Readiness health check degraded/unhealthy: {Errors}", string.Join("; ", errors));
            return HealthCheckResult.Unhealthy(string.Join("; ", errors));
        }

        return HealthCheckResult.Healthy("All dependencies reachable.");
    }
}
