using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.MessagingV2.Connections;
using OrderService.Infrastructure.MessagingV2.ConsumerServices;
using OrderService.Infrastructure.MessagingV2.Inbox;
using OrderService.Infrastructure.MessagingV2.Outbox;
using OrderService.Infrastructure.MessagingV2.Publish;
using OrderService.Infrastructure.MessagingV2.Serialization;
using OrderService.Infrastructure.MessagingV2.Topology;

namespace OrderService.Infrastructure.Dependency;

/// <summary>
/// Messaging DI split from feature folders: core (outbox + connections + publish) vs consumer jobs (optional extension method).
/// </summary>
public static class MessagingV2Registration
{
    /// <summary>
    /// Registers RabbitMQ provider options, TCP connections, channel pool, outbox/inbox stores, JSON serializer, routing registry,
    /// application <see cref="IEventPublisher"/>, topology initializer service, and the outbox dispatcher background job.
    /// 
    /// Startup order is enforced: RabbitMqTopologyInitializerHostedService runs first, initializing all topology.
    /// OutboxDispatcherJob and consumer jobs wait for topology initialization before beginning work.
    /// Does not register consumer hosted services — use <see cref="AddOrderMessagingConsumerJobs"/> for those.
    /// </summary>
    public static IServiceCollection AddMessagingV2(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection("Messaging"))
            .Validate(o => o.Providers.Count > 0, "At least one RabbitMQ provider must be configured under Messaging:Providers.")
            .ValidateOnStart();

        // Singleton: one TCP connection registry + channel pool shared by dispatcher (and optional publishers).
        services.AddSingleton<IRabbitMqConnectionRegistry, RabbitMqConnectionRegistry>();
        services.AddSingleton<IChannelPool, RabbitMqChannelPool>();
        services.AddSingleton<IEventRoutingRegistry, EventRoutingRegistry>();
        services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();

        // Topology initialization services (registered as singletons for startup coordination)
        services.AddSingleton<TopologyInitializationCoordinator>();
        services.AddSingleton<RabbitMqTopologyConfigurator>();
        services.AddSingleton<RabbitMqTopologyInitializer>();
        
        // Topology initializer runs first, before all other background services
        services.AddHostedService<RabbitMqTopologyInitializerHostedService>();

        // Scoped: same lifetime as DbContext for transactional outbox writes in requests.
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<IInboxStore, InboxStore>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        // Dispatcher waits for topology initialization before starting
        services.AddHostedService<OutboxDispatcherJob>();

        return services;
    }

    /// <summary>
    /// Registers queue consumer background jobs and their <see cref="IConsumer{T}"/> handlers.
    /// Kept separate so you can enable/disable consumers without touching core messaging registration.
    /// Each consumer waits for topology initialization before consuming.
    /// </summary>
    public static IServiceCollection AddOrderMessagingConsumerJobs(this IServiceCollection services)
    {
        services.AddScoped<OrderCreatedConsumer>();
        services.AddHostedService<OrderCreatedConsumerJob>();
        return services;
    }
}
