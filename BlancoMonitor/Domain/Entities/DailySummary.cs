namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Pre-computed daily aggregate statistics for a UrlSet.
/// One row per UrlSet per calendar date.
/// Enables fast dashboard rendering and trend charts without scanning all PageVisits.
/// </summary>
public sealed class DailySummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UrlSetId { get; set; }
    public DateTime Date { get; set; }
    public int TotalRuns { get; set; }
    public int TotalPageVisits { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public double P95ResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public double MinResponseTimeMs { get; set; }
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int WarningIssues { get; set; }
    public double AvailabilityPercent { get; set; }
    public long TotalDataTransferredBytes { get; set; }

    // Navigation
    public UrlSet? UrlSet { get; set; }
}
