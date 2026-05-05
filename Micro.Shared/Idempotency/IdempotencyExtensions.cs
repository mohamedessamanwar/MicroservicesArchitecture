using Microsoft.Extensions.DependencyInjection;

namespace Micro.Shared.Idempotency;

public static class IdempotencyExtensions
{
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        services.AddScoped<IdempotencyService>();
        return services;
    }
}