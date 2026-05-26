using System.Data;
using Micro.Shared.MetricServices.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Micro.Shared.MetricServices.Services;

public sealed class DbContextMonitoringDbConnectionFactory<TDbContext> : IMonitoringDbConnectionFactory
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DbContextMonitoringDbConnectionFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<DbConnectionContext?> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return new DbConnectionContext(connection, scope);
    }
}
