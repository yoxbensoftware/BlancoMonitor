using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Analysis;

public sealed class PerformanceAnalyzerImpl : IPerformanceAnalyzer
{
    public PerformanceMetric Analyze(IReadOnlyList<NetworkTrace> traces, string url)
    {
        if (traces.Count == 0)
            return new PerformanceMetric { Url = url };

        // Classify all requests
        string? pageHost = null;
        try { pageHost = new Uri(url).Host; } catch { }
        RequestClassifier.ClassifyAll(traces, pageHost);

        // Separate signal from noise
        var (signal, noise) = NoiseFilter.Partition(traces);
        var allTraces = traces; // keep full list for counting

        // Primary document trace (first HTML request)
        var primaryTrace = traces.FirstOrDefault(t => t.Category == RequestCategory.Html);
        var successfulSignal = signal.Where(t => t.ErrorMessage is null).ToList();

        // Detect page type
        var pageType = PageTypeDetector.Detect(url);

        // ── Compute basic timings ───────────────────────────────
        var ttfb = primaryTrace?.TimeToFirstByteMs ?? 0;
        var contentDownload = primaryTrace?.ContentDownloadMs ?? 0;
        var primaryTotal = primaryTrace?.TotalTimeMs ?? 0;

        // Full page load = time from first request to last request completion
        var firstTimestamp = allTraces.Min(t => t.Timestamp);
        var lastCompletion = allTraces.Max(t => t.Timestamp.AddMilliseconds(t.TotalTimeMs));
        var fullPageLoadMs = (lastCompletion - firstTimestamp).TotalMilliseconds;

        // DOM ready estimate = primary HTML + blocking CSS + synchronous JS
        var criticalResourceTime = signal
            .Where(t => t.Category is RequestCategory.Css or RequestCategory.JavaScript && !t.IsThirdParty)
            .Select(t => t.TotalTimeMs)
            .DefaultIfEmpty(0)
            .Max();
        var domReadyEstimate = primaryTotal + criticalResourceTime;

        // ── Resource breakdown ──────────────────────────────────
        var breakdowns = new Dictionary<RequestCategory, ResourceBreakdown>();
        foreach (var group in allTraces.GroupBy(t => t.Category))
        {
            breakdowns[group.Key] = new ResourceBreakdown
            {
                Count = group.Count(),
                TotalDurationMs = group.Sum(t => t.TotalTimeMs),
                TotalBytes = group.Sum(t => t.ContentLength),
                FailedCount = group.Count(t => t.ErrorMessage is not null || t.StatusCode >= 400),
            };
        }

        // ── API-specific metrics ────────────────────────────────
        var apiTraces = signal.Where(t => t.Category == RequestCategory.Api && t.ErrorMessage is null).ToList();
        var apiDurations = apiTraces.Select(t => t.TotalTimeMs).OrderBy(t => t).ToList();

        // ── Slowest endpoints ───────────────────────────────────
        var slowest = signal
            .Where(t => t.ErrorMessage is null)
            .OrderByDescending(t => t.TotalTimeMs)
            .Take(5)
            .Select(t => new SlowEndpoint
            {
                Url = t.Url,
                DurationMs = t.TotalTimeMs,
                StatusCode = t.StatusCode,
                Category = t.Category,
            })
            .ToList();

        return new PerformanceMetric
        {
            Url = url,
            TimeToFirstByteMs = ttfb,
            ContentDownloadMs = contentDownload,
            TotalTimeMs = primaryTotal,
            DomReadyEstimateMs = domReadyEstimate,
            FullPageLoadMs = fullPageLoadMs,
            StatusCode = primaryTrace?.StatusCode ?? 0,
            ContentLength = primaryTrace?.ContentLength ?? 0,
            Timestamp = DateTime.UtcNow,
            PageType = pageType,

            TotalRequestCount = allTraces.Count,
            FailedRequestCount = allTraces.Count(t => t.ErrorMessage is not null || t.StatusCode >= 400),
            ThirdPartyRequestCount = allTraces.Count(t => t.IsThirdParty),
            TotalTransferBytes = allTraces.Sum(t => t.ContentLength),

            ResourceBreakdowns = breakdowns,

            ApiRequestCount = apiTraces.Count,
            ApiAvgDurationMs = apiDurations.Count > 0 ? apiDurations.Average() : 0,
            ApiMaxDurationMs = apiDurations.Count > 0 ? apiDurations.Max() : 0,
            ApiP95DurationMs = Percentile(apiDurations, 95),
            SlowestEndpoints = slowest,
        };
    }

    public PerformanceStatistics ComputeStatistics(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return new PerformanceStatistics(0, 0, 0, 0, 0, 0, 0);

        var sorted = values.OrderBy(v => v).ToList();

        return new PerformanceStatistics(
            AverageMs: sorted.Average(),
            MedianMs: Percentile(sorted, 50),
            P95Ms: Percentile(sorted, 95),
            P99Ms: Percentile(sorted, 99),
            MaxMs: sorted[^1],
            MinMs: sorted[0],
            SampleCount: sorted.Count);
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        if (sorted.Count == 1) return sorted[0];

        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        var fraction = index - lower;

        if (lower == upper || upper >= sorted.Count)
            return sorted[lower];

        return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
    }
}
