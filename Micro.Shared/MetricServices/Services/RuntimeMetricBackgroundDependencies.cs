using Micro.Shared.MetricServices.Abstractions;

namespace Micro.Shared.MetricServices.Services;

public sealed class RuntimeMetricBackgroundDependencies
{
    public RuntimeMetricBackgroundDependencies(
        IRuntimeMetricSnapshotService snapshotService)
    {
        SnapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
    }

    public IRuntimeMetricSnapshotService SnapshotService { get; }
}
