using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Options;
using Micro.Shared.MetricServices.Services;
using Micro.Shared.MetricServices.Services.MetricesServices;

namespace Micro.Shared.MetricServices.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddMetricServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<MetricMonitoringOptions>(
            configuration.GetSection("MetricMonitoring"));

        // Register individual metric services
        services.AddScoped<ICpuMetricService, CpuMetricService>();
        services.AddScoped<IRamMetricService, RamMetricService>();
        services.AddScoped<IGarbageCollectorMetricService, GarbageCollectorMetricService>();
        services.AddScoped<IThreadPoolMetricService, ThreadPoolMetricService>();
        services.AddScoped<ISocketMetricService, SocketMetricService>();
        services.AddScoped<IDatabaseConnectionMetricService, DatabaseConnectionMetricService>();

        // Register composite services 
        services.AddScoped<IRuntimeMetricSnapshotService, RuntimeMetricSnapshotService>();

        // Register background 
        services.AddHostedService<RuntimeMetricBackgroundService>();

        return services;
    }
    
    /// <summary>
    /// Registers database connection factory for monitoring purposes with a specific DbContext type.
    /// </summary>
    public static IServiceCollection AddMonitoringDbConnectionFactory<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddTransient<IMonitoringDbConnectionFactory>(sp =>
            new DbContextMonitoringDbConnectionFactory<TDbContext>(sp.GetRequiredService<IServiceScopeFactory>()));
        return services;
    }
}
