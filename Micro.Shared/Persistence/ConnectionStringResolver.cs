using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Micro.Shared.Persistence;

public class ConnectionStringResolver : IConnectionStringResolver
{
    private readonly IConfiguration _configuration;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<ConnectionStringResolver> _logger;

    public ConnectionStringResolver(
        IConfiguration configuration,
        IRequestContext requestContext,
        ILogger<ConnectionStringResolver> logger)
    {
        _configuration = configuration;
        _requestContext = requestContext;
        _logger = logger;
    }

    public string Resolve()
    {
        var country = _requestContext.Country;
        var mode = _requestContext.OperationMode == OperationMode.Write ? "Primary" : "Replica";

        // Logic: ConnectionStrings:{Country}:{Primary|Replica}
        var connectionStringPath = $"ConnectionStrings:{country}:{mode}";
        var connectionString = _configuration[connectionStringPath];

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Connection string not found for Country: {Country}, Mode: {Mode} (Path: {Path})",
                country, mode, connectionStringPath);
            throw new InvalidOperationException($"Connection string for {country} ({mode}) is not configured.");
        }

        _logger.LogInformation("Resolved connection string for Country: {Country}, Mode: {Mode}", country, mode);

        return connectionString;
    }
}
