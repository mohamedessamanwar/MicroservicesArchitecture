using System.Threading;
using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Micro.Shared.MetricServices.Services;

public sealed class NpgsqlMonitoringDbConnectionFactory : IMonitoringDbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<MetricMonitoringOptions> _options;
    private readonly ILogger<NpgsqlMonitoringDbConnectionFactory> _logger;
    private int _missingConnectionWarned;

    public NpgsqlMonitoringDbConnectionFactory(
        IConfiguration configuration,
        IOptionsMonitor<MetricMonitoringOptions> options,
        ILogger<NpgsqlMonitoringDbConnectionFactory> logger)
    {
        _configuration = configuration;
        _options = options;
        _logger = logger;
    }

    public async Task<DbConnectionContext?> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var connectionString = options.DatabaseConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString) &&
            !string.IsNullOrWhiteSpace(options.DatabaseConnectionStringName))
        {
            connectionString = _configuration.GetConnectionString(options.DatabaseConnectionStringName);
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (Interlocked.Exchange(ref _missingConnectionWarned, 1) == 0)
            {
                _logger.LogWarning(
                    "Metric monitoring DB metrics are enabled but no connection string is configured. Set MetricMonitoring:DatabaseConnectionStringName or MetricMonitoring:DatabaseConnectionString.");
            }

            return null;
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return new DbConnectionContext(connection, scope: null);
    }
}
