using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// An issue detected during monitoring.
/// Enhanced version of the runtime Alert model with category, confidence,
/// and dual foreign keys (PageVisit + RunSession) for flexible querying.
/// </summary>
public sealed class DetectedIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PageVisitId { get; set; }
    public Guid RunSessionId { get; set; }
    public Severity Severity { get; set; }
    public IssueCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double ActualValue { get; set; }
    public double ThresholdValue { get; set; }
    public double Confidence { get; set; } = 1.0;
    public string Url { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public PageVisit? PageVisit { get; set; }
    public RunSession? RunSession { get; set; }
}
