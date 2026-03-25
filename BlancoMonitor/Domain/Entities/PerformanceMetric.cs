using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

public sealed class PerformanceMetric
{
    public string Url { get; set; } = string.Empty;
    public double TimeToFirstByteMs { get; set; }
    public double ContentDownloadMs { get; set; }
    public double TotalTimeMs { get; set; }
    public double DomReadyEstimateMs { get; set; }
    public double FullPageLoadMs { get; set; }
    public int StatusCode { get; set; }
    public long ContentLength { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public PageType PageType { get; set; } = PageType.Unknown;

    // Request breakdown
    public int TotalRequestCount { get; set; }
    public int FailedRequestCount { get; set; }
    public int ThirdPartyRequestCount { get; set; }
    public long TotalTransferBytes { get; set; }

    // Per-category breakdown: Category → (count, totalMs, totalBytes)
    public Dictionary<RequestCategory, ResourceBreakdown> ResourceBreakdowns { get; set; } = [];

    // API-specific durations
    public double ApiAvgDurationMs { get; set; }
    public double ApiMaxDurationMs { get; set; }
    public double ApiP95DurationMs { get; set; }
    public int ApiRequestCount { get; set; }
    public List<SlowEndpoint> SlowestEndpoints { get; set; } = [];
}

public sealed class ResourceBreakdown
{
    public int Count { get; set; }
    public double TotalDurationMs { get; set; }
    public double AvgDurationMs => Count > 0 ? TotalDurationMs / Count : 0;
    public long TotalBytes { get; set; }
    public int FailedCount { get; set; }
}

public sealed class SlowEndpoint
{
    public string Url { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public int StatusCode { get; set; }
    public RequestCategory Category { get; set; }
}
