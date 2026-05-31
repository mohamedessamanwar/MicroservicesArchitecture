using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.RabbitImplementation.Connections;
using OrderService.Infrastructure.RabbitImplementation.Consumers;
using OrderService.Infrastructure.RabbitImplementation.Topology;

using OrderService.Infrastructure.RabbitImplementation.Inbox;
using OrderService.Infrastructure.RabbitImplementation.Outbox;

using OrderService.Infrastructure.RabbitImplementation.Serialization;

namespace OrderService.Infrastructure.Dependency;

/// <summary>
/// Messaging DI split from feature folders: core (outbox + connections + publish) vs consumer jobs (optional extension method).
/// </summary>
public static class RabbitImplementationRegistration
{
    /// <summary>
    /// Registers RabbitMQ provider options, TCP connections, channel pool, outbox/inbox stores, JSON serializer, routing registry,
    /// application <see cref="IEventPublisher"/>, topology initializer job, and the outbox dispatcher background job.
    /// 
    /// Startup order is enforced: RabbitMqTopologyInitializerJob runs first, initializing all topology from configuration.
    /// OutboxDispatcherJob and consumer jobs wait for topology initialization before beginning work.
    /// Does not register consumer hosted services - use <see cref="AddRabbitImplementationConsumerJobs"/> for those.
    /// </summary>
    public static IServiceCollection AddRabbitImplementation(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection("Messaging"))
            .Validate(o => o.Countries.Count > 0, "At least one country must be configured under Messaging:Countries.")
            .Validate(o => o.Providers.Count > 0, "At least one RabbitMQ provider must be configured under Messaging:Providers.")
            .Validate(o => o.Topologies.Count > 0, "At least one RabbitMQ topology must be configured under Messaging:Topologies.")
            .ValidateOnStart();

        // Singleton: one TCP connection registry + channel pool shared by dispatcher (and optional publishers).
        services.AddSingleton<IRabbitMqConnectionRegistry, RabbitMqConnectionRegistry>();
        services.AddSingleton<IChannelPool, RabbitMqChannelPool>();
        services.AddSingleton<IEventRoutingRegistry, EventRoutingRegistry>();
        services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        services.AddSingleton<IRabbitMqNamePrefixer, RabbitMqNamePrefixer>();
        // Scoped: same lifetime as DbContext for transactional outbox writes in requests.
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<IInboxStore, InboxStore>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IEventConsumerResolver, EventConsumerResolver>();
        services.AddScoped<OrderCreatedEventConsumer>();

        services.AddSingleton<IHostedService, RabbitMqTopologyInitializerJob>();

        // Dispatcher waits for topology initialization before starting.
        // Register one hosted job per country so each dispatcher resolves the proper scoped connection string.
        services.AddSingleton<IHostedService>(sp => new OutboxDispatcherJob(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IChannelPool>(),
            sp.GetRequiredService<IRabbitMqNamePrefixer>(),
            sp.GetRequiredService<ILogger<OutboxDispatcherJob>>(),
            "Egypt"));

        services.AddSingleton<IHostedService>(sp => new OutboxDispatcherJob(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IChannelPool>(),
            sp.GetRequiredService<IRabbitMqNamePrefixer>(),
            sp.GetRequiredService<ILogger<OutboxDispatcherJob>>(),
            "UAE"));

        services.AddSingleton<IHostedService>(sp => new OrderCreatedConsumerJob(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IRabbitMqConnectionRegistry>(),
            sp.GetRequiredService<IRabbitMqNamePrefixer>(),
            sp.GetRequiredService<ILogger<OrderCreatedConsumerJob>>(),
            "Egypt"));

        services.AddSingleton<IHostedService>(sp => new OrderCreatedConsumerJob(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IRabbitMqConnectionRegistry>(),
            sp.GetRequiredService<IRabbitMqNamePrefixer>(),
            sp.GetRequiredService<ILogger<OrderCreatedConsumerJob>>(),
            "UAE"));

        return services;
    }

    /// <summary>
    /// Registers queue consumer background jobs and their <see cref="IConsumer{T}"/> handlers.
    /// Kept separate so you can enable/disable consumers without touching core messaging registration.
    /// Each consumer waits for topology initialization before consuming.
    /// </summary>
    public static IServiceCollection AddRabbitImplementationConsumerJobs(this IServiceCollection services)
    {
        return services;
    }
}

