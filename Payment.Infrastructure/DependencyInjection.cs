using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Data;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Repositories;
using Micro.Shared.Persistence;
using Micro.Shared.MetricServices.Extensions;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSharedPersistence();
        services.AddAppDbContext<AppDbContext>();

        // Register metric services
        services.AddMetricServices(configuration);
        services.AddMonitoringDbConnectionFactory<AppDbContext>();

        services.AddScoped<IPaymentRepository, EfPaymentRepository>();

        return services;
    }
}