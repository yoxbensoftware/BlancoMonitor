using BlancoMonitor.Application.Dto;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Application.Services;

/// <summary>
/// Assembles a comprehensive ReportData model from SQLite data for a given run session.
/// Includes performance stats, issues, resource breakdown, regressions, and actionable findings.
/// </summary>
public sealed class ReportDataBuilder
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;

    public ReportDataBuilder(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<ReportData> BuildAsync(Guid runSessionId, CancellationToken ct = default)
    {
        var session = await _database.GetRunSessionAsync(runSessionId, ct)
            ?? throw new InvalidOperationException($"RunSession {runSessionId} not found");

        var totalRuns = await _database.GetTotalRunCountAsync(ct);
        var pages = await _database.GetPageVisitsByRunAsync(runSessionId, ct);
        var issues = await _database.GetIssuesByRunAsync(runSessionId, ct);
        var comparisons = await _database.GetComparisonsAsync(runSessionId, ct);

        // Load all network requests for this run
        var allRequests = new List<NetworkRequest>();
        foreach (var page in pages)
        {
            var reqs = await _database.GetNetworkRequestsAsync(page.Id, ct);
            allRequests.AddRange(reqs);
        }

        var report = new ReportData
        {
            RunSessionId = runSessionId,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            Status = session.Status.ToString(),
            TotalRuns = totalRuns,
            TotalPagesVisited = pages.Count,
            TotalNetworkRequests = allRequests.Count,
            TotalBytesTransferred = allRequests.Sum(r => r.ContentLength),
            SuccessCount = session.SuccessCount,
            FailureCount = session.FailureCount,
            WarningCount = session.WarningCount,
            CriticalCount = session.CriticalCount,
        };

        // ── Performance summary ─────────────────────────────────
        BuildPerformanceSummary(report, pages);

        // ── Page entries ────────────────────────────────────────
        BuildPageEntries(report, pages, issues, allRequests);

        // ── Slowest pages / endpoints ───────────────────────────
        BuildSlowestEntries(report, pages, allRequests);

        // ── Issues ──────────────────────────────────────────────
        BuildIssueEntries(report, issues);

        // ── Resource breakdown ──────────────────────────────────
        BuildResourceBreakdown(report, allRequests);

        // ── Time distribution ───────────────────────────────────
        BuildTimeDistribution(report, pages);

        // ── Regressions / improvements ──────────────────────────
        await BuildRegressionDataAsync(report, comparisons, ct);

        // ── Actionable findings ─────────────────────────────────
        BuildActionableFindings(report, pages, issues, allRequests);

        return report;
    }

    private static void BuildPerformanceSummary(ReportData report, List<PageVisit> pages)
    {
        var times = pages
            .Where(p => p.Success && p.TotalTimeMs > 0)
            .Select(p => p.TotalTimeMs)
            .OrderBy(t => t)
            .ToList();

        if (times.Count == 0) return;

        report.AverageResponseMs = times.Average();
        report.MinResponseMs = times[0];
        report.MaxResponseMs = times[^1];
        report.MedianResponseMs = times[times.Count / 2];
        report.P95ResponseMs = times[(int)(times.Count * 0.95)];
        report.AverageTtfbMs = pages
            .Where(p => p.Success && p.TimeToFirstByteMs > 0)
            .Select(p => p.TimeToFirstByteMs)
            .DefaultIfEmpty(0)
            .Average();

        report.NoticeCount = report.TotalPagesVisited - report.SuccessCount - report.FailureCount;
    }

    private static void BuildPageEntries(ReportData report, List<PageVisit> pages,
        List<DetectedIssue> issues, List<NetworkRequest> allRequests)
    {
        foreach (var page in pages)
        {
            var pageAlerts = issues.Count(i => i.PageVisitId == page.Id);
            var pageRequests = allRequests.Count(r => r.PageVisitId == page.Id);

            report.Pages.Add(new PageReportEntry
            {
                Url = page.Url,
                StatusCode = page.StatusCode,
                TtfbMs = page.TimeToFirstByteMs,
                TotalTimeMs = page.TotalTimeMs,
                ContentDownloadMs = page.ContentDownloadMs,
                ContentLength = page.ContentLength,
                ContentType = page.ContentType,
                Success = page.Success,
                AlertCount = pageAlerts,
                RequestCount = pageRequests,
                ErrorMessage = page.ErrorMessage,
            });
        }
    }

    private static void BuildSlowestEntries(ReportData report, List<PageVisit> pages,
        List<NetworkRequest> allRequests)
    {
        report.SlowestPages = pages
            .Where(p => p.Success)
            .OrderByDescending(p => p.TotalTimeMs)
            .Take(10)
            .Select(p => new PageReportEntry
            {
                Url = p.Url,
                StatusCode = p.StatusCode,
                TtfbMs = p.TimeToFirstByteMs,
                TotalTimeMs = p.TotalTimeMs,
                ContentLength = p.ContentLength,
                Success = true,
            })
            .ToList();

        // Find the page URL for each request
        var pageUrlById = pages.ToDictionary(p => p.Id, p => p.Url);

        report.SlowestEndpoints = allRequests
            .Where(r => r.TotalTimeMs > 0)
            .OrderByDescending(r => r.TotalTimeMs)
            .Take(15)
            .Select(r => new EndpointReportEntry
            {
                Url = r.Url,
                DurationMs = r.TotalTimeMs,
                StatusCode = r.StatusCode,
                Category = GuessCategory(r.ContentType, r.Url),
                PageUrl = pageUrlById.TryGetValue(r.PageVisitId, out var pageUrl) ? pageUrl : "—",
            })
            .ToList();
    }

    private static void BuildIssueEntries(ReportData report, List<DetectedIssue> issues)
    {
        foreach (var issue in issues.OrderByDescending(i => i.Severity))
        {
            report.Issues.Add(new IssueReportEntry
            {
                Severity = issue.Severity.ToString(),
                Category = issue.Category.ToString(),
                Title = issue.Title,
                Description = issue.Description,
                Url = issue.Url,
                ActualValue = issue.ActualValue,
                ThresholdValue = issue.ThresholdValue,
                Confidence = issue.Confidence,
                Timestamp = issue.Timestamp,
            });
        }

        // Group by category
        report.IssuesByCategory = issues
            .GroupBy(i => i.Category.ToString())
            .Select(g => new IssueGroupEntry
            {
                Category = g.Key,
                Count = g.Count(),
                CriticalCount = g.Count(i => i.Severity == Severity.Critical),
                WarningCount = g.Count(i => i.Severity == Severity.Warning),
            })
            .OrderByDescending(g => g.CriticalCount)
            .ThenByDescending(g => g.WarningCount)
            .ToList();
    }

    private static void BuildResourceBreakdown(ReportData report, List<NetworkRequest> allRequests)
    {
        var groups = allRequests
            .GroupBy(r => GuessCategory(r.ContentType, r.Url));

        foreach (var g in groups)
        {
            report.ResourceBreakdown.Add(new ResourceCategoryEntry
            {
                Category = g.Key,
                RequestCount = g.Count(),
                TotalBytes = g.Sum(r => r.ContentLength),
                AvgDurationMs = g.Average(r => r.TotalTimeMs),
                FailedCount = g.Count(r => r.StatusCode >= 400),
            });
        }

        report.ResourceBreakdown = report.ResourceBreakdown
            .OrderByDescending(r => r.RequestCount).ToList();
    }

    private static void BuildTimeDistribution(ReportData report, List<PageVisit> pages)
    {
        var buckets = new (string Label, double Min, double Max)[]
        {
            ("< 200ms",     0,     200),
            ("200–500ms",   200,   500),
            ("500ms–1s",    500,   1000),
            ("1–2s",        1000,  2000),
            ("2–5s",        2000,  5000),
            ("5–10s",       5000,  10000),
            ("> 10s",       10000, double.MaxValue),
        };

        var total = pages.Count(p => p.Success && p.TotalTimeMs > 0);
        if (total == 0) return;

        foreach (var (label, min, max) in buckets)
        {
            var count = pages.Count(p => p.Success && p.TotalTimeMs >= min && p.TotalTimeMs < max);
            report.ResponseTimeDistribution.Add(new TimeBucketEntry
            {
                Bucket = label,
                Count = count,
                PercentOfTotal = total > 0 ? (double)count / total * 100 : 0,
            });
        }
    }

    private async Task BuildRegressionDataAsync(ReportData report,
        List<BaselineComparison> comparisons, CancellationToken ct)
    {
        if (comparisons.Count == 0) return;

        var baselineRunId = comparisons.First().BaselineRunSessionId;
        if (baselineRunId.HasValue)
        {
            var baselineSession = await _database.GetRunSessionAsync(baselineRunId.Value, ct);
            report.BaselineRunSessionId = baselineRunId;
            report.BaselineStartedAt = baselineSession?.StartedAt;
        }

        foreach (var comp in comparisons)
        {
            if (comp.Trend == TrendDirection.Degrading && comp.DeltaPercent > 10)
            {
                report.Regressions.Add(new RegressionEntry
                {
                    Url = comp.Url,
                    CurrentMs = comp.CurrentAvgMs,
                    BaselineMs = comp.BaselineAvgMs,
                    DeltaMs = comp.DeltaMs,
                    DeltaPercent = comp.DeltaPercent,
                    Trend = comp.Trend.ToString(),
                });
            }
            else if (comp.Trend == TrendDirection.Improving && comp.DeltaPercent < -10)
            {
                report.Improvements.Add(new ImprovementEntry
                {
                    Url = comp.Url,
                    CurrentMs = comp.CurrentAvgMs,
                    BaselineMs = comp.BaselineAvgMs,
                    DeltaMs = comp.DeltaMs,
                    DeltaPercent = comp.DeltaPercent,
                });
            }
        }

        report.Regressions = report.Regressions.OrderByDescending(r => r.DeltaPercent).ToList();
        report.Improvements = report.Improvements.OrderBy(i => i.DeltaPercent).ToList();

        // Overall delta
        if (comparisons.Count > 0)
        {
            var avgCurrent = comparisons.Average(c => c.CurrentAvgMs);
            var avgBaseline = comparisons.Where(c => c.BaselineAvgMs > 0).Select(c => c.BaselineAvgMs).DefaultIfEmpty(0).Average();
            report.OverallDeltaPercent = avgBaseline > 0 ? (avgCurrent - avgBaseline) / avgBaseline * 100 : 0;
        }
    }

    private static void BuildActionableFindings(ReportData report, List<PageVisit> pages,
        List<DetectedIssue> issues, List<NetworkRequest> allRequests)
    {
        // Finding 1: Critical issues
        var criticalIssues = issues.Where(i => i.Severity == Severity.Critical).ToList();
        if (criticalIssues.Count > 0)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "HIGH",
                Category = "Availability",
                Title = $"{criticalIssues.Count} critical issue(s) detected",
                Description = "Critical issues indicate serious performance or availability problems that require immediate attention.",
                Recommendation = "Investigate the critical alerts below. Focus on pages with HTTP 5xx errors or extreme response times.",
                AffectedUrls = criticalIssues.Select(i => i.Url).Distinct().ToList(),
            });
        }

        // Finding 2: Slow pages (> 5s)
        var slowPages = pages.Where(p => p.Success && p.TotalTimeMs > 5000).ToList();
        if (slowPages.Count > 0)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "HIGH",
                Category = "Performance",
                Title = $"{slowPages.Count} page(s) exceed 5-second load time",
                Description = $"Average load time for slow pages: {slowPages.Average(p => p.TotalTimeMs):F0}ms. Users typically abandon pages after 3 seconds.",
                Recommendation = "Optimize server-side processing, enable caching, and reduce blocking resources. Consider lazy-loading non-critical assets.",
                AffectedUrls = slowPages.Select(p => p.Url).ToList(),
            });
        }

        // Finding 3: High failure rate
        var failRate = pages.Count > 0 ? (double)pages.Count(p => !p.Success) / pages.Count * 100 : 0;
        if (failRate > 5)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "HIGH",
                Category = "Availability",
                Title = $"High failure rate: {failRate:F1}%",
                Description = $"{pages.Count(p => !p.Success)} of {pages.Count} page visits failed.",
                Recommendation = "Check server health, DNS resolution, and SSL certificates. Review error messages for patterns.",
                AffectedUrls = pages.Where(p => !p.Success).Select(p => p.Url).Distinct().ToList(),
            });
        }

        // Finding 4: Large pages
        var largePages = pages.Where(p => p.ContentLength > 2 * 1024 * 1024).ToList();
        if (largePages.Count > 0)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "MEDIUM",
                Category = "Performance",
                Title = $"{largePages.Count} page(s) exceed 2 MB content size",
                Description = "Large page sizes increase load time, especially on mobile networks.",
                Recommendation = "Compress images (WebP/AVIF), minify CSS/JS, enable GZIP/Brotli compression, and remove unused resources.",
                AffectedUrls = largePages.Select(p => p.Url).ToList(),
            });
        }

        // Finding 5: Slow TTFB
        var slowTtfb = pages.Where(p => p.Success && p.TimeToFirstByteMs > 1500).ToList();
        if (slowTtfb.Count > 0)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "MEDIUM",
                Category = "Performance",
                Title = $"{slowTtfb.Count} page(s) have TTFB > 1.5 seconds",
                Description = "High Time To First Byte indicates server-side processing delays.",
                Recommendation = "Investigate database query performance, add server-side caching, and consider a CDN for static content.",
                AffectedUrls = slowTtfb.Select(p => p.Url).ToList(),
            });
        }

        // Finding 6: Many failed sub-resources
        var failedResources = allRequests.Count(r => r.StatusCode >= 400);
        if (failedResources > 5)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "MEDIUM",
                Category = "Content",
                Title = $"{failedResources} broken sub-resource(s) detected",
                Description = "Failed resources (404s, 500s) create poor user experience and waste bandwidth.",
                Recommendation = "Fix broken links, update asset references, and remove references to deleted resources.",
                AffectedUrls = allRequests
                    .Where(r => r.StatusCode >= 400)
                    .Select(r => r.Url).Distinct().Take(10).ToList(),
            });
        }

        // Finding 7: Regressions
        if (report.Regressions.Count > 0)
        {
            report.ActionableFindings.Add(new FindingEntry
            {
                Priority = "HIGH",
                Category = "Regression",
                Title = $"{report.Regressions.Count} performance regression(s) vs previous run",
                Description = $"Worst regression: {report.Regressions.First().Url} — {report.Regressions.First().DeltaPercent:+0.0}% slower.",
                Recommendation = "Compare recent deployments or infrastructure changes. Check for added scripts, larger images, or database issues.",
                AffectedUrls = report.Regressions.Select(r => r.Url).ToList(),
            });
        }
    }

    private static string GuessCategory(string? contentType, string url)
    {
        if (string.IsNullOrEmpty(contentType)) return "Other";
        if (contentType.Contains("html")) return "Document";
        if (contentType.Contains("css")) return "Stylesheet";
        if (contentType.Contains("javascript") || contentType.Contains("ecmascript")) return "Script";
        if (contentType.Contains("image")) return "Image";
        if (contentType.Contains("font") || url.Contains(".woff") || url.Contains(".ttf")) return "Font";
        if (contentType.Contains("json") || contentType.Contains("xml")) return "API";
        if (contentType.Contains("video") || contentType.Contains("audio")) return "Media";
        return "Other";
    }
}
