using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.Domain.ValueObjects;

namespace BlancoMonitor.Infrastructure.Analysis;

/// <summary>
/// Advanced rule engine with:
/// - Per-page-type thresholds (Homepage, Product, Search, etc.)
/// - Four severity levels (Info, Notice, Warning, Critical)
/// - Three confidence levels (Suspected, Confirmed, Persistent)
/// - Noise filtering (analytics, fonts, tracking pixels excluded)
/// - Issue grouping (repeated identical issues are merged)
/// - Slow endpoint detection
/// - Request count analysis
/// - API-specific performance rules
/// </summary>
public sealed class RuleEngineImpl : IRuleEngine
{
    // ── Legacy method (backward compatible) ─────────────────────

    public List<Alert> Evaluate(PerformanceMetric metric, IReadOnlyDictionary<string, Threshold> thresholds)
    {
        var alerts = new List<Alert>();

        var metricValues = new Dictionary<string, double>
        {
            ["TotalTimeMs"] = metric.TotalTimeMs,
            ["TimeToFirstByteMs"] = metric.TimeToFirstByteMs,
            ["ContentDownloadMs"] = metric.ContentDownloadMs,
        };

        foreach (var (name, threshold) in thresholds)
        {
            if (!metricValues.TryGetValue(name, out var actual))
                continue;

            var exceeds = threshold.Operator switch
            {
                ComparisonOperator.GreaterThan => actual > threshold.CriticalValue,
                ComparisonOperator.LessThan => actual < threshold.CriticalValue,
                _ => false,
            };

            if (exceeds)
            {
                alerts.Add(new Alert
                {
                    Severity = Severity.Critical,
                    Message = $"{name} = {actual:F0}ms exceeds critical threshold ({threshold.CriticalValue:F0}ms)",
                    MetricName = name,
                    ActualValue = actual,
                    ThresholdValue = threshold.CriticalValue,
                    Url = metric.Url,
                });
                continue;
            }

            var warns = threshold.Operator switch
            {
                ComparisonOperator.GreaterThan => actual > threshold.WarningValue,
                ComparisonOperator.LessThan => actual < threshold.WarningValue,
                _ => false,
            };

            if (warns)
            {
                alerts.Add(new Alert
                {
                    Severity = Severity.Warning,
                    Message = $"{name} = {actual:F0}ms exceeds warning threshold ({threshold.WarningValue:F0}ms)",
                    MetricName = name,
                    ActualValue = actual,
                    ThresholdValue = threshold.WarningValue,
                    Url = metric.Url,
                });
            }
        }

        if (metric.StatusCode >= 400)
        {
            alerts.Add(new Alert
            {
                Severity = metric.StatusCode >= 500 ? Severity.Critical : Severity.Warning,
                Message = $"HTTP {metric.StatusCode} response from {metric.Url}",
                MetricName = "StatusCode",
                ActualValue = metric.StatusCode,
                ThresholdValue = 400,
                Url = metric.Url,
            });
        }

        return alerts;
    }

    // ── Advanced method (new) ───────────────────────────────────

    public List<Alert> EvaluateAdvanced(
        PerformanceMetric metric,
        IReadOnlyList<NetworkTrace> traces,
        IReadOnlyDictionary<string, Threshold> thresholds,
        IReadOnlyList<Alert>? priorAlerts = null)
    {
        var alerts = new List<Alert>();

        // Get page-type-specific thresholds
        var pageThresholds = PageTypeThresholds.Defaults.TryGetValue(metric.PageType, out var pt)
            ? pt
            : PageTypeThresholds.Defaults[PageType.Unknown];

        // ── Rule 1: TTFB (Time to First Byte) ──────────────────
        CheckThreshold(alerts, metric.Url, "TimeToFirstByteMs",
            metric.TimeToFirstByteMs, pageThresholds.TtfbWarningMs, pageThresholds.TtfbCriticalMs,
            "TTFB");

        // ── Rule 2: Total page load time ────────────────────────
        CheckThreshold(alerts, metric.Url, "TotalTimeMs",
            metric.FullPageLoadMs > 0 ? metric.FullPageLoadMs : metric.TotalTimeMs,
            pageThresholds.TotalWarningMs, pageThresholds.TotalCriticalMs,
            "Page Load");

        // ── Rule 3: DOM Ready estimate ──────────────────────────
        if (metric.DomReadyEstimateMs > 0)
        {
            CheckThreshold(alerts, metric.Url, "DomReadyEstimateMs",
                metric.DomReadyEstimateMs,
                pageThresholds.TotalWarningMs * 0.7,
                pageThresholds.TotalCriticalMs * 0.7,
                "DOM Ready");
        }

        // ── Rule 4: API-specific performance ────────────────────
        if (metric.ApiRequestCount > 0)
        {
            CheckThreshold(alerts, metric.Url, "ApiAvgDurationMs",
                metric.ApiAvgDurationMs,
                pageThresholds.ApiWarningMs, pageThresholds.ApiCriticalMs,
                "API Avg");

            if (metric.ApiP95DurationMs > pageThresholds.ApiCriticalMs * 1.5)
            {
                alerts.Add(CreateAlert(metric.Url, Severity.Critical, "ApiP95DurationMs",
                    metric.ApiP95DurationMs, pageThresholds.ApiCriticalMs,
                    $"API P95 latency = {metric.ApiP95DurationMs:F0}ms " +
                    $"(threshold: {pageThresholds.ApiCriticalMs:F0}ms)"));
            }
        }

        // ── Rule 5: HTTP status codes ───────────────────────────
        if (metric.StatusCode >= 500)
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Critical, "StatusCode",
                metric.StatusCode, 500,
                $"Server error HTTP {metric.StatusCode} from {metric.Url}"));
        }
        else if (metric.StatusCode >= 400)
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Warning, "StatusCode",
                metric.StatusCode, 400,
                $"Client error HTTP {metric.StatusCode} from {metric.Url}"));
        }

        // ── Rule 6: Failed request count ────────────────────────
        if (metric.FailedRequestCount > 0)
        {
            var failedPct = (double)metric.FailedRequestCount / Math.Max(1, metric.TotalRequestCount) * 100;
            var severity = failedPct switch
            {
                > 20 => Severity.Critical,
                > 5 => Severity.Warning,
                _ => Severity.Notice,
            };

            alerts.Add(CreateAlert(metric.Url, severity, "FailedRequestCount",
                metric.FailedRequestCount, 0,
                $"{metric.FailedRequestCount}/{metric.TotalRequestCount} requests failed ({failedPct:F1}%)"));
        }

        // ── Rule 7: Excessive request count ─────────────────────
        if (metric.TotalRequestCount > pageThresholds.MaxAcceptableRequestCount)
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Notice, "TotalRequestCount",
                metric.TotalRequestCount, pageThresholds.MaxAcceptableRequestCount,
                $"Excessive requests: {metric.TotalRequestCount} " +
                $"(recommended max: {pageThresholds.MaxAcceptableRequestCount} for {metric.PageType})"));
        }

        // ── Rule 8: Slow individual endpoints ───────────────────
        foreach (var slow in metric.SlowestEndpoints.Where(s => s.DurationMs > pageThresholds.ApiCriticalMs))
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Warning, "SlowEndpoint",
                slow.DurationMs, pageThresholds.ApiCriticalMs,
                $"Slow {slow.Category} endpoint: {TruncateUrl(slow.Url)} = {slow.DurationMs:F0}ms"));
        }

        // ── Rule 9: Third-party bloat ───────────────────────────
        if (metric.ThirdPartyRequestCount > metric.TotalRequestCount * 0.5 && metric.TotalRequestCount > 10)
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Info, "ThirdPartyRatio",
                metric.ThirdPartyRequestCount, metric.TotalRequestCount / 2,
                $"High 3rd-party ratio: {metric.ThirdPartyRequestCount}/{metric.TotalRequestCount} requests are third-party"));
        }

        // ── Rule 10: Large transfer size ────────────────────────
        var transferMb = metric.TotalTransferBytes / (1024.0 * 1024.0);
        if (transferMb > 5)
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Warning, "TotalTransferSize",
                transferMb, 5,
                $"Large page weight: {transferMb:F1}MB transferred"));
        }
        else if (transferMb > 3)
        {
            alerts.Add(CreateAlert(metric.Url, Severity.Notice, "TotalTransferSize",
                transferMb, 3,
                $"Page weight: {transferMb:F1}MB transferred"));
        }

        // ── Rule 11: Failed sub-resource requests (non-noise) ───
        var (signalTraces, _) = NoiseFilter.Partition(traces);
        var failedSignal = signalTraces.Where(t =>
            t.ErrorMessage is not null || t.StatusCode >= 400).ToList();
        foreach (var failed in failedSignal.Take(5))
        {
            var severity = failed.Category switch
            {
                RequestCategory.Api => Severity.Critical,
                RequestCategory.Html => Severity.Critical,
                RequestCategory.JavaScript => Severity.Warning,
                RequestCategory.Css => Severity.Warning,
                _ => Severity.Notice,
            };

            alerts.Add(CreateAlert(metric.Url, severity, $"FailedResource_{failed.Category}",
                failed.StatusCode > 0 ? failed.StatusCode : -1, 0,
                $"Failed {failed.Category}: {TruncateUrl(failed.Url)} " +
                $"({(failed.StatusCode > 0 ? $"HTTP {failed.StatusCode}" : failed.ErrorMessage ?? "Error")})"));
        }

        // ── Also run legacy threshold rules ─────────────────────
        alerts.AddRange(EvaluateLegacyThresholds(metric, thresholds));

        // ── Group + deduplicate + set confidence ────────────────
        return IssueGrouper.GroupAndDeduplicate(alerts, priorAlerts);
    }

    // ── Private helpers ─────────────────────────────────────────

    private List<Alert> EvaluateLegacyThresholds(
        PerformanceMetric metric,
        IReadOnlyDictionary<string, Threshold> thresholds)
    {
        var alerts = new List<Alert>();
        var metricValues = new Dictionary<string, double>
        {
            ["ContentDownloadMs"] = metric.ContentDownloadMs,
        };

        foreach (var (name, threshold) in thresholds)
        {
            if (!metricValues.TryGetValue(name, out var actual))
                continue;

            if (actual > threshold.CriticalValue)
            {
                alerts.Add(CreateAlert(metric.Url, Severity.Critical, name,
                    actual, threshold.CriticalValue,
                    $"{name} = {actual:F0}ms exceeds critical ({threshold.CriticalValue:F0}ms)"));
            }
            else if (actual > threshold.WarningValue)
            {
                alerts.Add(CreateAlert(metric.Url, Severity.Warning, name,
                    actual, threshold.WarningValue,
                    $"{name} = {actual:F0}ms exceeds warning ({threshold.WarningValue:F0}ms)"));
            }
        }

        return alerts;
    }

    private static void CheckThreshold(
        List<Alert> alerts, string url, string metricName,
        double actual, double warningValue, double criticalValue,
        string label)
    {
        if (actual > criticalValue)
        {
            alerts.Add(CreateAlert(url, Severity.Critical, metricName,
                actual, criticalValue,
                $"{label} = {actual:F0}ms exceeds critical threshold ({criticalValue:F0}ms)"));
        }
        else if (actual > warningValue)
        {
            alerts.Add(CreateAlert(url, Severity.Warning, metricName,
                actual, warningValue,
                $"{label} = {actual:F0}ms exceeds warning threshold ({warningValue:F0}ms)"));
        }
    }

    private static Alert CreateAlert(
        string url, Severity severity, string metricName,
        double actualValue, double thresholdValue, string message) => new()
    {
        Severity = severity,
        MetricName = metricName,
        ActualValue = actualValue,
        ThresholdValue = thresholdValue,
        Message = message,
        Url = url,
    };

    private static string TruncateUrl(string url) =>
        url.Length > 80 ? $"{url[..77]}..." : url;
}
