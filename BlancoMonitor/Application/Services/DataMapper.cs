using System.Text.Json;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Application.Services;

/// <summary>
/// Maps runtime in-memory models to persistence entities and vice-versa.
/// Centralizes all conversion logic between the two model families.
/// </summary>
public static class DataMapper
{
    // ── MonitoringSummary → RunSession ───────────────────────────

    public static RunSession ToRunSession(Dto.MonitoringSummary summary, string? name = null) => new()
    {
        StartedAt = summary.StartedAt,
        CompletedAt = summary.CompletedAt,
        Status = summary.Status,
        TotalUrls = summary.TotalUrlsChecked,
        SuccessCount = summary.SuccessCount,
        FailureCount = summary.FailureCount,
        WarningCount = summary.WarningCount,
        CriticalCount = summary.CriticalCount,
        AverageResponseTimeMs = summary.AverageResponseTimeMs,
        MaxResponseTimeMs = summary.MaxResponseTimeMs,
        TotalDurationMs = summary.TotalDuration.TotalMilliseconds,
        ReportPath = summary.ReportPath,
        Name = name,
    };

    // ── MonitoringResult → PageVisit ────────────────────────────

    public static PageVisit ToPageVisit(
        MonitoringResult result,
        Guid scenarioExecutionId,
        Guid runSessionId) => new()
    {
        Url = result.Url,
        ScenarioExecutionId = scenarioExecutionId,
        RunSessionId = runSessionId,
        StatusCode = result.Traces.FirstOrDefault()?.StatusCode ?? 0,
        TimeToFirstByteMs = result.Metrics?.TimeToFirstByteMs ?? 0,
        ContentDownloadMs = result.Metrics?.ContentDownloadMs ?? 0,
        TotalTimeMs = result.Metrics?.TotalTimeMs ?? 0,
        ContentLength = result.Traces.FirstOrDefault()?.ContentLength ?? 0,
        ContentType = result.Traces.FirstOrDefault()?.ContentType,
        Success = result.Success,
        ErrorMessage = result.ErrorMessage,
        Timestamp = result.Timestamp,
        DurationMs = result.Duration.TotalMilliseconds,
    };

    // ── NetworkTrace → NetworkRequest ───────────────────────────

    public static NetworkRequest ToNetworkRequest(NetworkTrace trace, Guid pageVisitId) => new()
    {
        PageVisitId = pageVisitId,
        Url = trace.Url,
        Method = trace.Method,
        StatusCode = trace.StatusCode,
        TimeToFirstByteMs = trace.TimeToFirstByteMs,
        TotalTimeMs = trace.TotalTimeMs,
        ContentDownloadMs = trace.ContentDownloadMs,
        ContentType = trace.ContentType,
        ContentLength = trace.ContentLength,
        RequestHeadersJson = trace.RequestHeaders.Count > 0
            ? JsonSerializer.Serialize(trace.RequestHeaders)
            : null,
        ResponseHeadersJson = trace.ResponseHeaders.Count > 0
            ? JsonSerializer.Serialize(trace.ResponseHeaders)
            : null,
        RedirectChainJson = trace.RedirectChain.Count > 0
            ? JsonSerializer.Serialize(trace.RedirectChain)
            : null,
        ErrorMessage = trace.ErrorMessage,
        Timestamp = trace.Timestamp,
    };

    // ── Alert → DetectedIssue ───────────────────────────────────

    public static DetectedIssue ToDetectedIssue(
        Alert alert,
        Guid pageVisitId,
        Guid runSessionId) => new()
    {
        PageVisitId = pageVisitId,
        RunSessionId = runSessionId,
        Severity = alert.Severity,
        Category = CategorizeMetric(alert.MetricName),
        Title = $"{alert.Severity}: {alert.MetricName}",
        Description = alert.Message,
        MetricName = alert.MetricName,
        ActualValue = alert.ActualValue,
        ThresholdValue = alert.ThresholdValue,
        Confidence = alert.Confidence switch
        {
            ConfidenceLevel.Persistent => 1.0,
            ConfidenceLevel.Confirmed => 0.9,
            ConfidenceLevel.Suspected => 0.6,
            _ => 0.5,
        },
        Url = alert.Url,
        Timestamp = alert.Timestamp,
    };

    // ── MonitorTarget → UrlSet + UrlSetEntry ────────────────────

    public static UrlSet ToUrlSet(MonitorTarget target) => new()
    {
        Id = target.Id,
        Name = target.Name,
        BaseUrl = target.Url,
        IsActive = target.IsEnabled,
        CheckIntervalSeconds = target.CheckIntervalSeconds,
    };

    public static UrlSetEntry ToUrlSetEntry(string url, Guid urlSetId, bool isDiscovered) => new()
    {
        UrlSetId = urlSetId,
        Url = url,
        IsDiscovered = isDiscovered,
        IsActive = true,
    };

    // ── Screenshot → EvidenceItem ───────────────────────────────

    public static EvidenceItem ToScreenshotEvidence(string filePath, Guid pageVisitId)
    {
        long fileSize = 0;
        try { fileSize = new FileInfo(filePath).Length; } catch { }

        return new EvidenceItem
        {
            PageVisitId = pageVisitId,
            Type = EvidenceType.Screenshot,
            FilePath = filePath,
            Description = "Page screenshot",
            FileSizeBytes = fileSize,
        };
    }

    // ── BaselineComparison builder ──────────────────────────────

    public static BaselineComparison BuildComparison(
        string url,
        Guid currentRunId,
        Guid? baselineRunId,
        List<PageVisit> currentVisits,
        List<PageVisit> baselineVisits)
    {
        var currentTimes = currentVisits.Select(v => v.TotalTimeMs).OrderBy(t => t).ToList();
        var baselineTimes = baselineVisits.Select(v => v.TotalTimeMs).OrderBy(t => t).ToList();

        var currentAvg = currentTimes.Count > 0 ? currentTimes.Average() : 0;
        var baselineAvg = baselineTimes.Count > 0 ? baselineTimes.Average() : 0;
        var deltaMs = currentAvg - baselineAvg;
        var deltaPct = baselineAvg > 0 ? (deltaMs / baselineAvg) * 100 : 0;

        var trend = baselineTimes.Count == 0
            ? TrendDirection.Insufficient
            : deltaPct switch
            {
                < -5 => TrendDirection.Improving,
                > 5 => TrendDirection.Degrading,
                _ => TrendDirection.Stable,
            };

        return new BaselineComparison
        {
            Url = url,
            RunSessionId = currentRunId,
            BaselineRunSessionId = baselineRunId,
            CurrentAvgMs = currentAvg,
            CurrentP95Ms = Percentile(currentTimes, 95),
            CurrentMaxMs = currentTimes.Count > 0 ? currentTimes.Max() : 0,
            CurrentStatusCode = currentVisits.LastOrDefault()?.StatusCode ?? 0,
            BaselineAvgMs = baselineAvg,
            BaselineP95Ms = Percentile(baselineTimes, 95),
            BaselineMaxMs = baselineTimes.Count > 0 ? baselineTimes.Max() : 0,
            BaselineStatusCode = baselineVisits.LastOrDefault()?.StatusCode ?? 0,
            DeltaMs = deltaMs,
            DeltaPercent = deltaPct,
            Trend = trend,
        };
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static IssueCategory CategorizeMetric(string metricName) =>
        metricName.ToLowerInvariant() switch
        {
            var n when n.Contains("timeout") => IssueCategory.Timeout,
            var n when n.Contains("status") => IssueCategory.StatusCode,
            var n when n.Contains("redirect") => IssueCategory.Redirect,
            var n when n.Contains("content") => IssueCategory.Content,
            var n when n.Contains("security") || n.Contains("ssl") => IssueCategory.Security,
            var n when n.Contains("available") || n.Contains("uptime") => IssueCategory.Availability,
            _ => IssueCategory.Performance,
        };

    private static double Percentile(List<double> sorted, int percentile)
    {
        if (sorted.Count == 0) return 0;
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}
