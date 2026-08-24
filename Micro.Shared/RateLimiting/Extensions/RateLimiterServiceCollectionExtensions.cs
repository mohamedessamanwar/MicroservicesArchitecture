using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micro.Shared.RateLimiting.Models;
using Micro.Shared.RateLimiting.Middleware;

namespace Micro.Shared.RateLimiting.Extensions;

public static class RateLimiterServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedRateLimiter(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string configSection = "RateLimiting")
    {
        // 1. Bind Options
        var optionsSection = configuration.GetSection(configSection);
        services.Configure<RateLimitOptions>(optionsSection);
        
        var options = optionsSection.Get<RateLimitOptions>() ?? new RateLimitOptions();

        // 2. Register Forwarded Headers (Crucial for IP Rate Limiting behind Gateway)
        services.Configure<ForwardedHeadersOptions>(forwardedOptions =>
        {
            forwardedOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            
            // Clear default networks to explicitly trust our config
            forwardedOptions.KnownNetworks.Clear();
            forwardedOptions.KnownProxies.Clear();

            foreach (var proxyCidr in options.KnownProxies)
            {
                if (System.Net.IPNetwork.TryParse(proxyCidr, out var network))
                {
                    forwardedOptions.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(network.BaseAddress, network.PrefixLength));
                }
                else if (IPAddress.TryParse(proxyCidr, out var address))
                {
                    forwardedOptions.KnownProxies.Add(address);
                }
            }
        });

        // 3. Register the Redis Rate Limiter implementation
        // Note: Assumes IConnectionMultiplexer is already registered by the shared caching/persistence setup
        services.AddSingleton<IRateLimiter, RedisTokenBucketRateLimiter>();

        return services;
    }

    public static IApplicationBuilder UseDistributedRateLimiter(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
}
