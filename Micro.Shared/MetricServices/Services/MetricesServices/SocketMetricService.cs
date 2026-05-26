using System.Net.NetworkInformation;
using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;

namespace Micro.Shared.MetricServices.Services.MetricesServices;

public sealed class SocketMetricService : ISocketMetricService
{
    public Task<SocketMetricSummary> GetSocketSummaryAsync(CancellationToken cancellationToken = default)
    {
        var connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

        // OS TCP stats expose IP/port, not the original domain; reverse DNS is avoided for overhead.
        var connectionMetrics = connections
            .Select(connection => new SocketConnectionMetric
            {
                LocalEndpoint = connection.LocalEndPoint.ToString(),
                RemoteEndpoint = connection.RemoteEndPoint.ToString(),
                State = connection.State.ToString(),
                RemoteIp = connection.RemoteEndPoint.Address.ToString(),
                RemotePort = connection.RemoteEndPoint.Port
            })
            .ToList();

        var groups = connections
            .GroupBy(connection => new
            {
                RemoteEndpoint = connection.RemoteEndPoint.ToString(),
                State = connection.State.ToString()
            })
            .Select(group => new SocketConnectionGroup
            {
                RemoteEndpoint = group.Key.RemoteEndpoint,
                State = group.Key.State,
                ConnectionCount = group.Count()
            })
            .OrderByDescending(group => group.ConnectionCount)
            .ToList();

        var summary = new SocketMetricSummary
        {
            CapturedAtUtc = DateTime.UtcNow,
            TotalConnections = connections.Length,
            Connections = connectionMetrics,
            Groups = groups
        };

        return Task.FromResult(summary);
    }
}
