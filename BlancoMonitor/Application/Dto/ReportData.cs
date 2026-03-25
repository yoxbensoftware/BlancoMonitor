using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Application.Dto;

/// <summary>
/// Complete report data model assembled from DB data.
/// Used by all report generators (HTML, JSON, CSV) as the single source of truth.
/// </summary>
public sealed class ReportData
{
    // ── Metadata ────────────────────────────────────────────────
    public string ReportId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratorVersion { get; set; } = "1.0.0";

    // ── Run overview ────────────────────────────────────────────
    public Guid RunSessionId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
    public string Status { get; set; } = "Unknown";

    // ── Aggregate counts ────────────────────────────────────────
    public int TotalRuns { get; set; }
    public int TotalPagesVisited { get; set; }
    public int TotalNetworkRequests { get; set; }
    public long TotalBytesTransferred { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
    public int NoticeCount { get; set; }

    // ── Performance summary ─────────────────────────────────────
    public double AverageResponseMs { get; set; }
    public double MedianResponseMs { get; set; }
    public double P95ResponseMs { get; set; }
    public double MaxResponseMs { get; set; }
    public double MinResponseMs { get; set; }
    public double AverageTtfbMs { get; set; }

    // ── Page results ────────────────────────────────────────────
    public List<PageReportEntry> Pages { get; set; } = [];
    public List<PageReportEntry> SlowestPages { get; set; } = [];
    public List<EndpointReportEntry> SlowestEndpoints { get; set; } = [];

    // ── Issues ──────────────────────────────────────────────────
    public List<IssueReportEntry> Issues { get; set; } = [];
    public List<IssueGroupEntry> IssuesByCategory { get; set; } = [];

    // ── Resource breakdown ──────────────────────────────────────
    public List<ResourceCategoryEntry> ResourceBreakdown { get; set; } = [];

    // ── Time-based analysis ─────────────────────────────────────
    public List<TimeBucketEntry> ResponseTimeDistribution { get; set; } = [];

    // ── Baseline comparison / regressions ───────────────────────
    public Guid? BaselineRunSessionId { get; set; }
    public DateTime? BaselineStartedAt { get; set; }
    public List<RegressionEntry> Regressions { get; set; } = [];
    public List<ImprovementEntry> Improvements { get; set; } = [];
    public double OverallDeltaPercent { get; set; }

    // ── Actionable findings ─────────────────────────────────────
    public List<FindingEntry> ActionableFindings { get; set; } = [];
}

// ── Supporting models ───────────────────────────────────────

public sealed class PageReportEntry
{
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public double TtfbMs { get; set; }
    public double TotalTimeMs { get; set; }
    public double ContentDownloadMs { get; set; }
    public long ContentLength { get; set; }
    public string? ContentType { get; set; }
    public bool Success { get; set; }
    public int AlertCount { get; set; }
    public int RequestCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class EndpointReportEntry
{
    public string Url { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public int StatusCode { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
}

public sealed class IssueReportEntry
{
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public double ActualValue { get; set; }
    public double ThresholdValue { get; set; }
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class IssueGroupEntry
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
}

public sealed class ResourceCategoryEntry
{
    public string Category { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public long TotalBytes { get; set; }
    public double AvgDurationMs { get; set; }
    public int FailedCount { get; set; }
}

public sealed class TimeBucketEntry
{
    public string Bucket { get; set; } = string.Empty;
    public int Count { get; set; }
    public double PercentOfTotal { get; set; }
}

public sealed class RegressionEntry
{
    public string Url { get; set; } = string.Empty;
    public double CurrentMs { get; set; }
    public double BaselineMs { get; set; }
    public double DeltaMs { get; set; }
    public double DeltaPercent { get; set; }
    public string Trend { get; set; } = string.Empty;
}

public sealed class ImprovementEntry
{
    public string Url { get; set; } = string.Empty;
    public double CurrentMs { get; set; }
    public double BaselineMs { get; set; }
    public double DeltaMs { get; set; }
    public double DeltaPercent { get; set; }
}

public sealed class FindingEntry
{
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<string> AffectedUrls { get; set; } = [];
}
