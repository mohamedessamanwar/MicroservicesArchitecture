using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Micro.Shared.Caching;

public static class RedisCachingExtensions
{
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringKey = "Redis")
    {
        var connectionString = configuration.GetConnectionString(connectionStringKey)
            ?? throw new InvalidOperationException($"Redis connection string '{connectionStringKey}' not found in configuration");
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = 3;
            configurationOptions.ConnectTimeout = 5000;
            configurationOptions.SyncTimeout = 5000;
            configurationOptions.AsyncTimeout = 5000;
            configurationOptions.KeepAlive = 60;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });
        services.AddScoped<IRedisRepository, RedisRepository>();
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        Action<ConfigurationOptions> configureOptions)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
                AsyncTimeout = 5000,
                KeepAlive = 60
            };

            configureOptions(options);

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddScoped<IRedisRepository, RedisRepository>();
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}