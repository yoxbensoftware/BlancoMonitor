using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlancoMonitor.Application.Dto;
using BlancoMonitor.Application.Interfaces;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Reporting;

public sealed class HtmlReportGenerator : IReportGenerator, IMultiFormatReportGenerator
{
    // ── Legacy interface (unchanged) ────────────────────────────
    public async Task<string> GenerateAsync(List<MonitoringResult> results, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var filePath = Path.Combine(outputDirectory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<title>BlancoMonitor Report</title>");
        AppendCss(sb);
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>⬡ BlancoMonitor Report</h1>");
        sb.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        var totalUrls = results.Count;
        var totalAlerts = results.Sum(r => r.Alerts.Count);
        var criticalAlerts = results.Sum(r => r.Alerts.Count(a => a.Severity == Severity.Critical));
        var avgTime = results.Where(r => r.Metrics is not null).Select(r => r.Metrics!.TotalTimeMs).DefaultIfEmpty(0).Average();

        sb.AppendLine("<div class='summary'>");
        AppendCard(sb, "URLs Checked", totalUrls.ToString(), "ok");
        AppendCard(sb, "Avg Response", $"{avgTime:F0}ms", avgTime > 3000 ? "warning" : "ok");
        AppendCard(sb, "Alerts", totalAlerts.ToString(), totalAlerts > 0 ? "warning" : "ok");
        AppendCard(sb, "Critical", criticalAlerts.ToString(), criticalAlerts > 0 ? "critical" : "ok");
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>Results</h2>");
        sb.AppendLine("<table><tr><th>URL</th><th>Status</th><th>TTFB</th><th>Total</th><th>Size</th><th>Alerts</th></tr>");
        foreach (var result in results)
        {
            var m = result.Metrics;
            var alertClass = result.Alerts.Any(a => a.Severity == Severity.Critical) ? "critical" : result.Alerts.Count != 0 ? "warning" : "ok";
            sb.AppendLine($"<tr><td>{Esc(result.Url)}</td><td class='{(m?.StatusCode >= 400 ? "critical" : "ok")}'>{m?.StatusCode}</td><td>{m?.TimeToFirstByteMs:F0}</td><td>{m?.TotalTimeMs:F0}</td><td>{FormatSize(m?.ContentLength ?? 0)}</td><td class='{alertClass}'>{result.Alerts.Count}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), ct);
        return filePath;
    }

    // ── New: Full HTML report ───────────────────────────────────

    public async Task<string> GenerateHtmlAsync(ReportData data, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var filePath = Path.Combine(outputDirectory, $"report_{data.ReportId}_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var sb = new StringBuilder(32_000);
        sb.AppendLine("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'>");
        sb.AppendLine($"<title>BlancoMonitor Report — {data.StartedAt:yyyy-MM-dd HH:mm}</title>");
        sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
        AppendFullCss(sb);
        sb.AppendLine("</head><body>");

        // ── Header ──────────────────────────────────────────────
        sb.AppendLine("<header>");
        sb.AppendLine("<h1>⬡ BLANCO MONITOR — Run Report</h1>");
        sb.AppendLine($"<div class='meta'>Report ID: {Esc(data.ReportId)} | Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC | Status: <span class='ok'>{Esc(data.Status)}</span></div>");
        sb.AppendLine($"<div class='meta'>Run: {data.StartedAt:yyyy-MM-dd HH:mm} → {data.CompletedAt?.ToString("HH:mm:ss") ?? "—"} | Duration: {data.Duration:mm\\:ss} | Total historical runs: {data.TotalRuns}</div>");
        sb.AppendLine("</header>");

        // ── Summary cards ───────────────────────────────────────
        sb.AppendLine("<section><h2>Overview</h2><div class='summary'>");
        AppendCard(sb, "Pages", data.TotalPagesVisited.ToString(), "ok");
        AppendCard(sb, "Requests", data.TotalNetworkRequests.ToString(), "ok");
        AppendCard(sb, "Transfer", FormatSize(data.TotalBytesTransferred), "ok");
        AppendCard(sb, "Avg Response", $"{data.AverageResponseMs:F0}ms", data.AverageResponseMs > 3000 ? "warning" : "ok");
        AppendCard(sb, "P95", $"{data.P95ResponseMs:F0}ms", data.P95ResponseMs > 5000 ? "critical" : data.P95ResponseMs > 3000 ? "warning" : "ok");
        AppendCard(sb, "Warnings", data.WarningCount.ToString(), data.WarningCount > 0 ? "warning" : "ok");
        AppendCard(sb, "Critical", data.CriticalCount.ToString(), data.CriticalCount > 0 ? "critical" : "ok");

        // Overall trend
        if (data.BaselineRunSessionId.HasValue)
        {
            var trendClass = data.OverallDeltaPercent > 10 ? "critical" : data.OverallDeltaPercent > 0 ? "warning" : "ok";
            var arrow = data.OverallDeltaPercent > 0 ? "▲" : data.OverallDeltaPercent < 0 ? "▼" : "■";
            AppendCard(sb, "vs Baseline", $"{arrow} {data.OverallDeltaPercent:+0.0;-0.0}%", trendClass);
        }
        sb.AppendLine("</div></section>");

        // ── Performance stats ───────────────────────────────────
        sb.AppendLine("<section><h2>Performance Summary</h2><table>");
        sb.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
        sb.AppendLine($"<tr><td>Average Response Time</td><td>{data.AverageResponseMs:F0} ms</td></tr>");
        sb.AppendLine($"<tr><td>Median Response Time</td><td>{data.MedianResponseMs:F0} ms</td></tr>");
        sb.AppendLine($"<tr><td>P95 Response Time</td><td>{data.P95ResponseMs:F0} ms</td></tr>");
        sb.AppendLine($"<tr><td>Max Response Time</td><td>{data.MaxResponseMs:F0} ms</td></tr>");
        sb.AppendLine($"<tr><td>Min Response Time</td><td>{data.MinResponseMs:F0} ms</td></tr>");
        sb.AppendLine($"<tr><td>Average TTFB</td><td>{data.AverageTtfbMs:F0} ms</td></tr>");
        sb.AppendLine($"<tr><td>Success Rate</td><td>{(data.TotalPagesVisited > 0 ? (double)data.SuccessCount / data.TotalPagesVisited * 100 : 0):F1}%</td></tr>");
        sb.AppendLine("</table></section>");

        // ── Response time distribution ──────────────────────────
        if (data.ResponseTimeDistribution.Count > 0)
        {
            sb.AppendLine("<section><h2>Response Time Distribution</h2><table>");
            sb.AppendLine("<tr><th>Bucket</th><th>Count</th><th>%</th><th>Bar</th></tr>");
            foreach (var bucket in data.ResponseTimeDistribution)
            {
                var barWidth = (int)(bucket.PercentOfTotal * 3);
                var barColor = bucket.Bucket.Contains("> 10") || bucket.Bucket.Contains("5–10") ? "critical" : bucket.Bucket.Contains("2–5") ? "warning" : "ok";
                sb.AppendLine($"<tr><td>{Esc(bucket.Bucket)}</td><td>{bucket.Count}</td><td>{bucket.PercentOfTotal:F1}%</td><td><span class='bar {barColor}' style='width:{barWidth}px'></span></td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Slowest pages ───────────────────────────────────────
        if (data.SlowestPages.Count > 0)
        {
            sb.AppendLine("<section><h2>Slowest Pages (Top 10)</h2><table>");
            sb.AppendLine("<tr><th>#</th><th>URL</th><th>HTTP</th><th>TTFB</th><th>Total</th><th>Size</th></tr>");
            for (int i = 0; i < data.SlowestPages.Count; i++)
            {
                var p = data.SlowestPages[i];
                var cls = p.TotalTimeMs > 5000 ? "critical" : p.TotalTimeMs > 3000 ? "warning" : "ok";
                sb.AppendLine($"<tr><td>{i + 1}</td><td>{Esc(p.Url)}</td><td>{p.StatusCode}</td><td>{p.TtfbMs:F0}</td><td class='{cls}'>{p.TotalTimeMs:F0}</td><td>{FormatSize(p.ContentLength)}</td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Slowest endpoints ───────────────────────────────────
        if (data.SlowestEndpoints.Count > 0)
        {
            sb.AppendLine("<section><h2>Slowest Endpoints (Top 15)</h2><table>");
            sb.AppendLine("<tr><th>#</th><th>URL</th><th>HTTP</th><th>Duration</th><th>Type</th><th>Page</th></tr>");
            for (int i = 0; i < data.SlowestEndpoints.Count; i++)
            {
                var e = data.SlowestEndpoints[i];
                sb.AppendLine($"<tr><td>{i + 1}</td><td class='url-cell'>{Esc(e.Url)}</td><td>{e.StatusCode}</td><td>{e.DurationMs:F0}ms</td><td>{Esc(e.Category)}</td><td class='url-cell'>{Esc(e.PageUrl)}</td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Resource breakdown ──────────────────────────────────
        if (data.ResourceBreakdown.Count > 0)
        {
            sb.AppendLine("<section><h2>Resource Breakdown by Type</h2><table>");
            sb.AppendLine("<tr><th>Type</th><th>Requests</th><th>Total Size</th><th>Avg Duration</th><th>Failed</th></tr>");
            foreach (var r in data.ResourceBreakdown)
            {
                sb.AppendLine($"<tr><td>{Esc(r.Category)}</td><td>{r.RequestCount}</td><td>{FormatSize(r.TotalBytes)}</td><td>{r.AvgDurationMs:F0}ms</td><td class='{(r.FailedCount > 0 ? "critical" : "ok")}'>{r.FailedCount}</td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Issues ──────────────────────────────────────────────
        if (data.Issues.Count > 0)
        {
            sb.AppendLine("<section><h2>Detected Issues</h2>");

            // Category summary
            if (data.IssuesByCategory.Count > 0)
            {
                sb.AppendLine("<div class='summary'>");
                foreach (var cat in data.IssuesByCategory)
                {
                    var cls = cat.CriticalCount > 0 ? "critical" : cat.WarningCount > 0 ? "warning" : "ok";
                    AppendCard(sb, cat.Category, $"{cat.Count} ({cat.CriticalCount}C / {cat.WarningCount}W)", cls);
                }
                sb.AppendLine("</div>");
            }

            sb.AppendLine("<table><tr><th>Sev</th><th>Category</th><th>Title</th><th>URL</th><th>Actual</th><th>Threshold</th><th>Confidence</th></tr>");
            foreach (var issue in data.Issues)
            {
                var cls = issue.Severity == "Critical" ? "critical" : issue.Severity == "Warning" ? "warning" : "dim";
                sb.AppendLine($"<tr><td class='{cls}'>{Esc(issue.Severity)}</td><td>{Esc(issue.Category)}</td><td>{Esc(issue.Title)}</td><td class='url-cell'>{Esc(issue.Url)}</td><td>{issue.ActualValue:F0}</td><td>{issue.ThresholdValue:F0}</td><td>{issue.Confidence:P0}</td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Regressions ─────────────────────────────────────────
        if (data.Regressions.Count > 0)
        {
            sb.AppendLine("<section><h2>⚠ Performance Regressions</h2>");
            sb.AppendLine($"<p class='dim'>Compared against baseline run: {data.BaselineStartedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—"}</p>");
            sb.AppendLine("<table><tr><th>URL</th><th>Current</th><th>Baseline</th><th>Delta</th><th>Change</th></tr>");
            foreach (var r in data.Regressions)
            {
                sb.AppendLine($"<tr><td class='url-cell'>{Esc(r.Url)}</td><td class='critical'>{r.CurrentMs:F0}ms</td><td>{r.BaselineMs:F0}ms</td><td class='critical'>+{r.DeltaMs:F0}ms</td><td class='critical'>▲ {r.DeltaPercent:+0.0}%</td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Improvements ────────────────────────────────────────
        if (data.Improvements.Count > 0)
        {
            sb.AppendLine("<section><h2>✓ Performance Improvements</h2>");
            sb.AppendLine("<table><tr><th>URL</th><th>Current</th><th>Baseline</th><th>Delta</th><th>Change</th></tr>");
            foreach (var imp in data.Improvements)
            {
                sb.AppendLine($"<tr><td class='url-cell'>{Esc(imp.Url)}</td><td class='ok'>{imp.CurrentMs:F0}ms</td><td>{imp.BaselineMs:F0}ms</td><td class='ok'>{imp.DeltaMs:F0}ms</td><td class='ok'>▼ {imp.DeltaPercent:0.0}%</td></tr>");
            }
            sb.AppendLine("</table></section>");
        }

        // ── Actionable findings ─────────────────────────────────
        if (data.ActionableFindings.Count > 0)
        {
            sb.AppendLine("<section><h2>Actionable Findings</h2>");
            foreach (var finding in data.ActionableFindings)
            {
                var cls = finding.Priority == "HIGH" ? "critical" : "warning";
                sb.AppendLine($"<div class='finding'>");
                sb.AppendLine($"<div class='finding-header'><span class='{cls}'>[{Esc(finding.Priority)}]</span> <strong>{Esc(finding.Title)}</strong> <span class='dim'>({Esc(finding.Category)})</span></div>");
                sb.AppendLine($"<p>{Esc(finding.Description)}</p>");
                sb.AppendLine($"<p class='recommendation'>💡 {Esc(finding.Recommendation)}</p>");
                if (finding.AffectedUrls.Count > 0)
                {
                    sb.AppendLine("<details><summary>Affected URLs</summary><ul>");
                    foreach (var url in finding.AffectedUrls.Take(10))
                        sb.AppendLine($"<li>{Esc(url)}</li>");
                    if (finding.AffectedUrls.Count > 10)
                        sb.AppendLine($"<li class='dim'>... and {finding.AffectedUrls.Count - 10} more</li>");
                    sb.AppendLine("</ul></details>");
                }
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</section>");
        }

        // ── All pages ───────────────────────────────────────────
        sb.AppendLine("<section><h2>All Pages</h2><table>");
        sb.AppendLine("<tr><th>URL</th><th>HTTP</th><th>TTFB</th><th>Download</th><th>Total</th><th>Size</th><th>Requests</th><th>Alerts</th><th>Status</th></tr>");
        foreach (var page in data.Pages)
        {
            var rowClass = !page.Success ? "critical" : page.AlertCount > 0 ? "warning" : "";
            sb.AppendLine($"<tr class='{rowClass}'><td class='url-cell'>{Esc(page.Url)}</td><td>{page.StatusCode}</td><td>{page.TtfbMs:F0}</td><td>{page.ContentDownloadMs:F0}</td><td>{page.TotalTimeMs:F0}</td><td>{FormatSize(page.ContentLength)}</td><td>{page.RequestCount}</td><td>{page.AlertCount}</td><td>{(page.Success ? "OK" : Esc(page.ErrorMessage ?? "FAIL"))}</td></tr>");
        }
        sb.AppendLine("</table></section>");

        // ── Footer ──────────────────────────────────────────────
        sb.AppendLine("<footer>");
        sb.AppendLine($"<p>BlancoMonitor v{Esc(data.GeneratorVersion)} — Generated {data.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC — Developed by Oz</p>");
        sb.AppendLine("</footer>");
        sb.AppendLine("</body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), ct);
        return filePath;
    }

    // ── JSON report ─────────────────────────────────────────────

    public async Task<string> GenerateJsonAsync(ReportData data, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var filePath = Path.Combine(outputDirectory, $"report_{data.ReportId}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync(filePath, json, ct);
        return filePath;
    }

    // ── CSV export ──────────────────────────────────────────────

    public async Task<string> GenerateCsvAsync(ReportData data, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var filePath = Path.Combine(outputDirectory, $"report_{data.ReportId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("URL,StatusCode,TTFB_ms,ContentDownload_ms,Total_ms,ContentLength_bytes,ContentType,Success,AlertCount,RequestCount,ErrorMessage");

        // Data rows
        foreach (var page in data.Pages)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(page.Url),
                page.StatusCode,
                page.TtfbMs.ToString("F1", CultureInfo.InvariantCulture),
                page.ContentDownloadMs.ToString("F1", CultureInfo.InvariantCulture),
                page.TotalTimeMs.ToString("F1", CultureInfo.InvariantCulture),
                page.ContentLength,
                CsvEscape(page.ContentType ?? ""),
                page.Success,
                page.AlertCount,
                page.RequestCount,
                CsvEscape(page.ErrorMessage ?? "")));
        }

        // Append issues sheet (blank line separator)
        sb.AppendLine();
        sb.AppendLine("# Issues");
        sb.AppendLine("Severity,Category,Title,URL,ActualValue,ThresholdValue,Confidence,Timestamp");
        foreach (var issue in data.Issues)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(issue.Severity),
                CsvEscape(issue.Category),
                CsvEscape(issue.Title),
                CsvEscape(issue.Url),
                issue.ActualValue.ToString("F1", CultureInfo.InvariantCulture),
                issue.ThresholdValue.ToString("F1", CultureInfo.InvariantCulture),
                issue.Confidence.ToString("F2", CultureInfo.InvariantCulture),
                issue.Timestamp.ToString("o")));
        }

        // Append regressions
        if (data.Regressions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("# Regressions");
            sb.AppendLine("URL,Current_ms,Baseline_ms,Delta_ms,DeltaPercent,Trend");
            foreach (var r in data.Regressions)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(r.Url),
                    r.CurrentMs.ToString("F1", CultureInfo.InvariantCulture),
                    r.BaselineMs.ToString("F1", CultureInfo.InvariantCulture),
                    r.DeltaMs.ToString("F1", CultureInfo.InvariantCulture),
                    r.DeltaPercent.ToString("F1", CultureInfo.InvariantCulture),
                    CsvEscape(r.Trend)));
            }
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), ct);
        return filePath;
    }

    // ── All formats ─────────────────────────────────────────────

    public async Task<ReportPaths> GenerateAllAsync(ReportData data, string outputDirectory, CancellationToken ct = default)
    {
        var paths = new ReportPaths
        {
            HtmlPath = await GenerateHtmlAsync(data, outputDirectory, ct),
            JsonPath = await GenerateJsonAsync(data, outputDirectory, ct),
            CsvPath = await GenerateCsvAsync(data, outputDirectory, ct),
        };
        return paths;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static void AppendCard(StringBuilder sb, string title, string value, string cssClass)
    {
        sb.AppendLine($"<div class='summary-card'><h3>{Esc(title)}</h3><div class='value {cssClass}'>{Esc(value)}</div></div>");
    }

    private static string Esc(string s) => System.Net.WebUtility.HtmlEncode(s);

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };

    private static void AppendCss(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine("body { background: #0a0a0a; color: #00ff41; font-family: 'Consolas', 'Courier New', monospace; padding: 20px; }");
        sb.AppendLine("h1, h2 { color: #39ff14; border-bottom: 1px solid #005000; padding-bottom: 8px; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin: 16px 0; }");
        sb.AppendLine("th, td { border: 1px solid #005000; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background: #001a00; color: #39ff14; }");
        sb.AppendLine("tr:hover { background: #001a00; }");
        sb.AppendLine(".critical { color: #ff3232; font-weight: bold; }");
        sb.AppendLine(".warning { color: #ffbf00; }");
        sb.AppendLine(".ok { color: #00ff41; }");
        sb.AppendLine(".summary { display: flex; gap: 20px; margin: 16px 0; flex-wrap: wrap; }");
        sb.AppendLine(".summary-card { background: #001a00; border: 1px solid #005000; padding: 16px; border-radius: 4px; flex: 1; min-width: 120px; }");
        sb.AppendLine(".summary-card h3 { margin: 0 0 8px 0; color: #39ff14; font-size: 11px; }");
        sb.AppendLine(".summary-card .value { font-size: 24px; }");
        sb.AppendLine("</style>");
    }

    private static void AppendFullCss(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine("""
            :root { --bg: #050505; --surface: #0c190c; --border: #005000; --text: #00ff41; --accent: #39ff14; --dim: #008c28; --warn: #ffbf00; --crit: #ff3232; }
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body { background: var(--bg); color: var(--text); font-family: 'Consolas', 'Courier New', monospace; padding: 24px 40px; line-height: 1.5; font-size: 13px; }
            header { border-bottom: 2px solid var(--border); padding-bottom: 16px; margin-bottom: 24px; }
            h1 { color: var(--accent); font-size: 22px; margin-bottom: 8px; }
            h2 { color: var(--accent); font-size: 16px; border-bottom: 1px solid var(--border); padding-bottom: 6px; margin-bottom: 12px; }
            section { margin-bottom: 32px; }
            .meta { color: var(--dim); font-size: 12px; margin: 2px 0; }
            table { border-collapse: collapse; width: 100%; margin: 12px 0; }
            th, td { border: 1px solid var(--border); padding: 6px 10px; text-align: left; font-size: 12px; }
            th { background: #001a00; color: var(--accent); font-weight: bold; }
            tr:nth-child(even) { background: rgba(0, 40, 0, 0.15); }
            tr:hover { background: #001a00; }
            .url-cell { max-width: 350px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
            .critical { color: var(--crit); font-weight: bold; }
            .warning { color: var(--warn); }
            .ok { color: var(--text); }
            .dim { color: var(--dim); }
            .summary { display: flex; gap: 14px; margin: 14px 0; flex-wrap: wrap; }
            .summary-card { background: var(--surface); border: 1px solid var(--border); padding: 14px; border-radius: 4px; flex: 1; min-width: 110px; }
            .summary-card h3 { margin: 0 0 6px; color: var(--dim); font-size: 10px; text-transform: uppercase; letter-spacing: 1px; }
            .summary-card .value { font-size: 22px; font-weight: bold; }
            .bar { display: inline-block; height: 14px; border-radius: 2px; }
            .bar.ok { background: var(--text); }
            .bar.warning { background: var(--warn); }
            .bar.critical { background: var(--crit); }
            .finding { background: var(--surface); border: 1px solid var(--border); padding: 16px; margin: 10px 0; border-radius: 4px; }
            .finding-header { margin-bottom: 8px; font-size: 14px; }
            .recommendation { color: var(--accent); margin-top: 8px; font-style: italic; }
            details { margin-top: 8px; }
            summary { cursor: pointer; color: var(--dim); }
            details ul { margin: 8px 0 0 20px; }
            details li { font-size: 11px; color: var(--dim); }
            footer { border-top: 1px solid var(--border); padding-top: 12px; margin-top: 40px; color: var(--dim); font-size: 11px; text-align: center; }
            """);
        sb.AppendLine("</style>");
    }
}
