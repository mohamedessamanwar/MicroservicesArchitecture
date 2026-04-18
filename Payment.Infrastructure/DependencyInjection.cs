using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Data;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Repositories;
using Micro.Shared.Persistence;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSharedPersistence();
        services.AddAppDbContext<AppDbContext>();

        services.AddScoped<IPaymentRepository, EfPaymentRepository>();

        return services;
    }
}