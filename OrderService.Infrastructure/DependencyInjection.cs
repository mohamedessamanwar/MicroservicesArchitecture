using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Interfaces;

using OrderService.Infrastructure.Messaging.RabbitMqConfiguration;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Dependency;
using Micro.Shared.Persistence;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        // Register shared infrastructure
        services.AddSharedPersistence();
        services.AddAppDbContext<AppDbContext>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.Configure<RabbitMqConfiguration>(
            configuration.GetSection(RabbitMqConfiguration.SectionName));

        //// Single connection per application (Singleton)
        //services.AddSingleton<IRabbitMQConnectionManager, RabbitMQConnectionManager>();
        //services.AddSingleton<RabbitMQTopologyInitializer>();
        //services.AddHostedService<RabbitMQTopologyHostedService>();

        //services.AddSingleton<ICdcEventHandler, OrdersCreateHandler>();
        //services.AddSingleton<ICdcEventHandler, OrdersUpdateHandler>();
        ////services.AddSingleton<ICdcEventHandler, OrdersDeleteHandler>();
        //services.AddSingleton<ICdcEventHandlerResolver, CdcEventHandlerResolver>();
        //services.AddSingleton<IMessageHandler, CdcMessageHandler>();
        //services.AddHostedService<CDCConsumerService>();
        services.AddMessagingV2(configuration);
        services.AddOrderMessagingConsumerJobs();
        //services.AddHostedService<RabbitMQConsumer>();
        //services.AddHostedService<RabbitMQDispatcher>();

        return services;
    }
}