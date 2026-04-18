using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Micro.Shared.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddSharedPersistence(this IServiceCollection services)
    {
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IConnectionStringResolver, ConnectionStringResolver>();
        return services;
    }

    public static IServiceCollection AddAppDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<IConnectionStringResolver>();
            var connectionString = resolver.Resolve();
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
