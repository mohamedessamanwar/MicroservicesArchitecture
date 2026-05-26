using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;
using Micro.Shared.MetricServices.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Micro.Shared.MetricServices.Services.MetricesServices;

public sealed class DatabaseConnectionMetricService : IDatabaseConnectionMetricService
{
    private const string Sql = @"
                                    select
                                        application_name,
                                        client_addr::text as client_addr,
                                        state,
                                        count(*) as connection_count
                                    from pg_stat_activity
                                    where datname = current_database()
                                    group by application_name, client_addr, state
                                    order by connection_count desc;";

    private readonly IMonitoringDbConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<MetricMonitoringOptions> _options;
    private readonly ILogger<DatabaseConnectionMetricService> _logger;

    public DatabaseConnectionMetricService(
        IMonitoringDbConnectionFactory connectionFactory,
        IOptionsMonitor<MetricMonitoringOptions> options,
        ILogger<DatabaseConnectionMetricService> logger)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<DatabaseConnectionSummary?> GetDatabaseConnectionSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.EnableDatabaseConnectionMetrics)
        {
            return null;
        }

        try
        {
            await using var context = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            if (context == null)
            {
                return null;
            }

            await using var command = context.Connection.CreateCommand();
            command.CommandText = Sql;

            var entries = new List<DatabaseConnectionMetric>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                entries.Add(new DatabaseConnectionMetric
                {
                    ApplicationName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    ClientAddress = reader.IsDBNull(1) ? null : reader.GetString(1),
                    State = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    ConnectionCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                });
            }

            var active = entries.Where(entry => entry.State.Equals("active", StringComparison.OrdinalIgnoreCase))
                .Sum(entry => entry.ConnectionCount);
            var idle = entries.Where(entry => entry.State.Equals("idle", StringComparison.OrdinalIgnoreCase))
                .Sum(entry => entry.ConnectionCount);
            var idleInTransaction = entries.Where(entry => entry.State.Equals("idle in transaction", StringComparison.OrdinalIgnoreCase))
                .Sum(entry => entry.ConnectionCount);

            // Active: currently running queries; Idle: connected and waiting; Idle in transaction: open transaction with no activity.
            // Set Application Name per microservice in the connection string to make attribution clearer.
            return new DatabaseConnectionSummary
            {
                CapturedAtUtc = DateTime.UtcNow,
                TotalConnections = entries.Sum(entry => entry.ConnectionCount),
                ActiveConnections = active,
                IdleConnections = idle,
                IdleInTransactionConnections = idleInTransaction,
                Entries = entries
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection metrics collection failed.");
            return null;
        }
    }
}
