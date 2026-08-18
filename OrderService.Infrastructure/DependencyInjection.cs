using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Interfaces;


using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Dependency;
using Micro.Shared.Persistence;
using OrderService.Infrastructure.Data;
using Micro.Shared.MetricServices.Abstractions;
using OrderService.Infrastructure.MetricServices;
using Micro.Shared.MetricServices.Extensions;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        // Register shared infrastructure
        services.AddSharedPersistence();
        services.AddAppDbContext<AppDbContext>();

        // Register metric services
        services.AddMetricServices(configuration);
        services.AddMonitoringDbConnectionFactory<AppDbContext>();

        services.AddScoped<IRuntimeMetricSnapshotRepository, RuntimeMetricSnapshotRepository>();
        services.AddScoped<ISpikeReportRepository, SpikeReportRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));


        services.AddRabbitImplementation(configuration);
        services.AddRabbitImplementationConsumerJobs();

        return services;
    }
}
