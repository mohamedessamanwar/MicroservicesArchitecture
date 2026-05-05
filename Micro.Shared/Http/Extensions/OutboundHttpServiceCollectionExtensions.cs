using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Configuration;
using Micro.Shared.Http.Handlers;
using Micro.Shared.Http.Policies;
using Micro.Shared.Http.Clients.Order;
using Micro.Shared.Http.Clients.Payment;

namespace Micro.Shared.Http.Extensions;

public static class OutboundHttpServiceCollectionExtensions
{
    public static IServiceCollection AddOutboundHttpInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
            .AddHttpMessageHandler(sp => new HeaderPropagationHandler(
                sp.GetRequiredService<IHttpContextAccessor>(),
                options.CallerIdentity))
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

        configuration.GetSection("OutboundHttp:CallerIdentity").Bind(options.CallerIdentity);
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

        if (string.IsNullOrWhiteSpace(options.CallerIdentity.AppId))
        {
            options.CallerIdentity.AppId =
                configuration["OutboundHttp:CallerIdentity:AppId"] ??
                configuration["ServiceIdentity:AppId"] ??
                configuration["App:AppId"] ??
                string.Empty;
        }

        if (string.IsNullOrWhiteSpace(options.CallerIdentity.AppId))
        {
            throw new InvalidOperationException(
                $"Missing outbound AppId for {clientName}. Configure OutboundHttp:CallerIdentity:AppId or OutboundHttp:Clients:{clientName}:CallerIdentity:AppId.");
        }

        return options;
    }
}