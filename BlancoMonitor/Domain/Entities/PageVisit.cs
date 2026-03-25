namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Records a single page visit with all performance metrics.
/// The core measurement unit — one row per URL visited.
/// Owns child NetworkRequests, DetectedIssues, and EvidenceItems.
/// </summary>
public sealed class PageVisit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScenarioExecutionId { get; set; }
    public Guid RunSessionId { get; set; }
    public string Url { get; set; } = string.Empty;

    // Performance metrics (inline, no separate table)
    public int StatusCode { get; set; }
    public double TimeToFirstByteMs { get; set; }
    public double ContentDownloadMs { get; set; }
    public double TotalTimeMs { get; set; }
    public long ContentLength { get; set; }
    public string? ContentType { get; set; }

    // Outcome
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double DurationMs { get; set; }

    // Navigation
    public ScenarioExecution? ScenarioExecution { get; set; }
    public List<NetworkRequest> NetworkRequests { get; set; } = [];
    public List<DetectedIssue> DetectedIssues { get; set; } = [];
    public List<EvidenceItem> EvidenceItems { get; set; } = [];
}
