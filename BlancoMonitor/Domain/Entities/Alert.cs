using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

public sealed class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Severity Severity { get; set; }
    public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Suspected;
    public string Message { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double ActualValue { get; set; }
    public double ThresholdValue { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Key used to group repeated identical issues (metric+severity+url-path).</summary>
    public string GroupKey { get; set; } = string.Empty;

    /// <summary>Number of occurrences when grouped.</summary>
    public int OccurrenceCount { get; set; } = 1;
}
