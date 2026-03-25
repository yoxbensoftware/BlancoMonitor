using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

public sealed class HistoricalRecord
{
    public string Url { get; set; } = string.Empty;
    public List<PerformanceMetric> Metrics { get; set; } = [];
    public PerformanceMetric? Baseline { get; set; }
    public TrendDirection Trend { get; set; } = TrendDirection.Insufficient;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
