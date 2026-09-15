namespace OrderService.Domain.Entities;

public sealed class SpikeReportRecord
{
    public Guid Id { get; set; }
    public Guid RuntimeMetricSnapshotId { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    public DateTime CorrelationWindowStartUtc { get; set; }
    public DateTime CorrelationWindowEndUtc { get; set; }
    public string Reasons { get; set; } = string.Empty;

    public RuntimeMetricSnapshotRecord Snapshot { get; set; } = default!;
}
