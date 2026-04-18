using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Micro.Shared.Clients.Order;
using Micro.Shared.Clients.Payment;
using Micro.Shared.Http.Configuration;
using Micro.Shared.Http.Handlers;
using Micro.Shared.Http.Policies;

namespace Micro.Shared.Http.Extensions;

public static class OutboundHttpServiceCollectionExtensions
{
    public static IServiceCollection AddOutboundHttpInfrastructure(this IServiceCollection services, string appId)
    {
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddTransient<HeaderPropagationHandler>(_ =>
            new HeaderPropagationHandler(
                _.GetRequiredService<IHttpContextAccessor>(),
                appId));

        return services;
    }

    public static IServiceCollection AddPaymentServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDownstreamClient<IPaymentServiceClient, PaymentServiceClient>(
            configuration,
            "PaymentService",
            "Services:PaymentService");
    }

    public static IServiceCollection AddOrderServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDownstreamClient<IOrderServiceClient, OrderServiceClient>(
            configuration,
            "OrderService",
            "Services:OrderService");
    }

    private static IServiceCollection AddDownstreamClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string clientName,
        string fallbackBaseUrlConfigKey)
        where TClient : class
        where TImplementation : class, TClient
    {
        var options = BuildClientOptions(configuration, clientName, fallbackBaseUrlConfigKey);

        services.AddHttpClient<TClient, TImplementation>((_, client) =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.OverallRequestTimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                MaxConnectionsPerServer = options.MaxConnectionsPerServer,
                PooledConnectionLifetime = TimeSpan.FromSeconds(options.PooledConnectionLifetimeSeconds),
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(options.PooledConnectionIdleTimeoutSeconds),
                ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds),
            })
            .AddHttpMessageHandler<HeaderPropagationHandler>()
            .AddPolicyHandler((sp, request) =>
            {
                var logger = sp.GetRequiredService<ILogger<TImplementation>>();
                var pipelineKey = ResiliencePipelineSelector.Resolve(request);

                return HttpClientResiliencePolicyFactory.GetOrCreate(
                    clientName,
                    pipelineKey,
                    options,
                    logger);
            });

        return services;
    }

    private static DownstreamHttpClientOptions BuildClientOptions(
        IConfiguration configuration,
        string clientName,
        string fallbackBaseUrlConfigKey)
    {
        var options = new DownstreamHttpClientOptions();

        configuration.GetSection("OutboundHttp:Defaults").Bind(options);
        configuration.GetSection($"OutboundHttp:Clients:{clientName}").Bind(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            options.BaseUrl = configuration[fallbackBaseUrlConfigKey] ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException(
                $"Missing outbound base URL for {clientName}. Configure OutboundHttp:Clients:{clientName}:BaseUrl or {fallbackBaseUrlConfigKey}.");
        }

        return options;
    }
}