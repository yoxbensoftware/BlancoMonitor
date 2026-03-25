using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Compares a URL's performance between two runs (current vs baseline).
/// One row per URL per comparison. Enables regression detection and trend reporting.
/// </summary>
public sealed class BaselineComparison
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public Guid RunSessionId { get; set; }
    public Guid? BaselineRunSessionId { get; set; }

    // Current run metrics
    public double CurrentAvgMs { get; set; }
    public double CurrentP95Ms { get; set; }
    public double CurrentMaxMs { get; set; }
    public int CurrentStatusCode { get; set; }

    // Baseline run metrics
    public double BaselineAvgMs { get; set; }
    public double BaselineP95Ms { get; set; }
    public double BaselineMaxMs { get; set; }
    public int BaselineStatusCode { get; set; }

    // Computed deltas
    public double DeltaMs { get; set; }
    public double DeltaPercent { get; set; }
    public TrendDirection Trend { get; set; } = TrendDirection.Insufficient;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public RunSession? RunSession { get; set; }
    public RunSession? BaselineRunSession { get; set; }
}
