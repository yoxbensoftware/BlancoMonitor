using BlancoMonitor.Application.Dto;
using BlancoMonitor.Application.Interfaces;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Application.Services;

public sealed class MonitoringOrchestrator
{
    private readonly IScenarioEngine _scenarioEngine;
    private readonly IDiscoveryEngine _discoveryEngine;
    private readonly IPerformanceAnalyzer _analyzer;
    private readonly IRuleEngine _ruleEngine;
    private readonly IEvidenceCollector _evidenceCollector;
    private readonly IReportGenerator _reportGenerator;
    private readonly IHistoricalStore _historicalStore;
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;
    private readonly ReportDataBuilder _reportDataBuilder;

    public MonitoringOrchestrator(
        IScenarioEngine scenarioEngine,
        IDiscoveryEngine discoveryEngine,
        IPerformanceAnalyzer analyzer,
        IRuleEngine ruleEngine,
        IEvidenceCollector evidenceCollector,
        IReportGenerator reportGenerator,
        IHistoricalStore historicalStore,
        IBlancoDatabase database,
        IAppLogger logger)
    {
        _scenarioEngine = scenarioEngine;
        _discoveryEngine = discoveryEngine;
        _analyzer = analyzer;
        _ruleEngine = ruleEngine;
        _evidenceCollector = evidenceCollector;
        _reportGenerator = reportGenerator;
        _historicalStore = historicalStore;
        _database = database;
        _logger = logger;
        _reportDataBuilder = new ReportDataBuilder(database, logger);
    }

    private const int LazyDelayMs = 10_000;
    private const int LazyMaxUrls = 8;

    public async Task<MonitoringSummary> RunAsync(
        AppConfiguration config,
        List<MonitorTarget> targets,
        IProgress<MonitoringProgress>? progress = null,
        CancellationToken ct = default,
        ManualResetEventSlim? pauseEvent = null,
        bool lazyMode = false)
    {
        var summary = new MonitoringSummary
        {
            StartedAt = DateTime.UtcNow,
            Status = MonitorStatus.Running,
        };

        // ── DB: Create RunSession ───────────────────────────────
        var runSession = new RunSession
        {
            StartedAt = summary.StartedAt,
            Status = MonitorStatus.Running,
        };
        await _database.InsertRunSessionAsync(runSession, ct);

        // ── Structured logging: set run context ────────────────
        (_logger as IStructuredLogger)?.SetRunContext(runSession.Id);

        _logger.Info($"=== Monitoring session started{(lazyMode ? " (LAZY MODE)" : "")}: {targets.Count} targets (RunSession {runSession.Id}) ===");

        var allUrls = new List<(MonitorTarget Target, string Url)>();

        // Phase 1: Discovery
        foreach (var target in targets.Where(t => t.IsEnabled))
        {
            ct.ThrowIfCancellationRequested();

            _logger.Info($"Discovering URLs for: {target.Name} ({target.Url})");
            progress?.Report(new MonitoringProgress
            {
                StatusMessage = $"Discovering: {target.Url}",
                CurrentUrl = target.Url,
            });

            try
            {
                var discovered = await _discoveryEngine.DiscoverUrlsAsync(target.Url, ct);
                var filtered = FilterUrls(discovered, config);
                foreach (var url in filtered)
                    allUrls.Add((target, url));

                _logger.Info($"  Found {filtered.Count} URLs for {target.Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Discovery failed for {target.Url}", ex);
                allUrls.Add((target, target.Url));
            }
        }

        summary.TotalUrlsChecked = allUrls.Count;

        // Lazy mode: randomly sample a small subset of discovered URLs
        if (lazyMode && allUrls.Count > LazyMaxUrls)
        {
            var rng = new Random();
            allUrls = allUrls.OrderBy(_ => rng.Next()).Take(LazyMaxUrls).ToList();
            summary.TotalUrlsChecked = allUrls.Count;
            _logger.Info($"🐢 Lazy mode: randomly selected {allUrls.Count} URLs from full set");
        }

        // ── DB: Ensure UrlSet records exist for each target ─────
        foreach (var target in targets.Where(t => t.IsEnabled))
        {
            var existing = await _database.GetUrlSetAsync(target.Id, ct);
            if (existing is null)
            {
                var urlSet = new UrlSet
                {
                    Id = target.Id,
                    Name = target.Name,
                    BaseUrl = target.Url,
                    IsActive = target.IsEnabled,
                    CheckIntervalSeconds = target.CheckIntervalSeconds,
                    CreatedAt = target.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                };
                await _database.InsertUrlSetAsync(urlSet, ct);
            }
        }

        // ── DB: Create ScenarioExecution per target ─────────────
        var executionsByTarget = new Dictionary<Guid, ScenarioExecution>();
        foreach (var target in targets.Where(t => t.IsEnabled))
        {
            var exec = new ScenarioExecution
            {
                RunSessionId = runSession.Id,
                UrlSetId = target.Id,
                StartedAt = DateTime.UtcNow,
                Status = MonitorStatus.Running,
                TotalPages = allUrls.Count(u => u.Target.Id == target.Id),
            };
            await _database.InsertScenarioExecutionAsync(exec, ct);
            executionsByTarget[target.Id] = exec;
        }

        // Phase 2: Monitoring
        for (var i = 0; i < allUrls.Count; i++)
        {
            // Wait if paused
            pauseEvent?.Wait(ct);

            ct.ThrowIfCancellationRequested();

            var (target, url) = allUrls[i];

            progress?.Report(new MonitoringProgress
            {
                CurrentIndex = i + 1,
                TotalCount = allUrls.Count,
                CurrentUrl = url,
                StatusMessage = $"[{i + 1}/{allUrls.Count}] Monitoring: {url}",
            });

            var result = await MonitorSingleUrlAsync(target, url, config, ct);
            summary.Results.Add(result);

            if (result.Success)
                summary.SuccessCount++;
            else
                summary.FailureCount++;

            summary.WarningCount += result.Alerts.Count(a => a.Severity == Severity.Warning);
            summary.CriticalCount += result.Alerts.Count(a => a.Severity == Severity.Critical);

            // ── DB: Persist PageVisit + children ────────────────
            if (executionsByTarget.TryGetValue(target.Id, out var execution))
            {
                await PersistPageVisitAsync(result, execution.Id, runSession.Id, ct);
                execution.PagesCompleted++;
            }

            if (i < allUrls.Count - 1)
            {
                var delayMs = lazyMode ? LazyDelayMs : config.DelayBetweenRequestsMs;
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }
        }

        // Phase 3: Compute summary statistics
        var responseTimes = summary.Results
            .Where(r => r.Metrics is not null)
            .Select(r => r.Metrics!.TotalTimeMs)
            .ToList();

        if (responseTimes.Count > 0)
        {
            summary.AverageResponseTimeMs = responseTimes.Average();
            summary.MaxResponseTimeMs = responseTimes.Max();
        }

        // Phase 4: Generate reports (HTML + JSON + CSV)
        try
        {
            var reportData = await _reportDataBuilder.BuildAsync(runSession.Id, ct);

            if (_reportGenerator is IMultiFormatReportGenerator multiReport)
            {
                var reportPaths = await multiReport.GenerateAllAsync(reportData, config.ReportDirectory, ct);
                summary.ReportPath = reportPaths.HtmlPath;
                _logger.Info($"Reports generated — HTML: {reportPaths.HtmlPath}, JSON: {reportPaths.JsonPath}, CSV: {reportPaths.CsvPath}");
            }
            else
            {
                summary.ReportPath = await _reportGenerator.GenerateAsync(summary.Results, config.ReportDirectory, ct);
                _logger.Info($"Report generated: {summary.ReportPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Report generation failed", ex);
        }

        summary.CompletedAt = DateTime.UtcNow;
        summary.Status = MonitorStatus.Completed;

        // ── DB: Finalize RunSession + ScenarioExecutions ────────
        runSession.CompletedAt = summary.CompletedAt;
        runSession.Status = MonitorStatus.Completed;
        runSession.TotalUrls = summary.TotalUrlsChecked;
        runSession.SuccessCount = summary.SuccessCount;
        runSession.FailureCount = summary.FailureCount;
        runSession.WarningCount = summary.WarningCount;
        runSession.CriticalCount = summary.CriticalCount;
        runSession.AverageResponseTimeMs = summary.AverageResponseTimeMs;
        runSession.MaxResponseTimeMs = summary.MaxResponseTimeMs;
        runSession.TotalDurationMs = summary.TotalDuration.TotalMilliseconds;
        runSession.ReportPath = summary.ReportPath;
        await _database.UpdateRunSessionAsync(runSession, ct);

        foreach (var exec in executionsByTarget.Values)
        {
            exec.CompletedAt = DateTime.UtcNow;
            exec.Status = MonitorStatus.Completed;
            exec.DurationMs = (exec.CompletedAt.Value - exec.StartedAt).TotalMilliseconds;
            await _database.UpdateScenarioExecutionAsync(exec, ct);
        }

        // ── DB: Baseline comparison against previous run ────────
        await BuildBaselineComparisonsAsync(runSession.Id, ct);

        _logger.Info($"=== Monitoring session completed: {summary.TotalUrlsChecked} URLs, " +
                     $"{summary.CriticalCount} critical, {summary.WarningCount} warnings, " +
                     $"avg {summary.AverageResponseTimeMs:F0}ms ===");

        // ── Clear structured logging context ────────────────────
        (_logger as IStructuredLogger)?.SetRunContext(null);

        return summary;
    }

    private async Task<MonitoringResult> MonitorSingleUrlAsync(
        MonitorTarget target,
        string url,
        AppConfiguration config,
        CancellationToken ct)
    {
        var result = new MonitoringResult
        {
            TargetId = target.Id,
            Url = url,
            Timestamp = DateTime.UtcNow,
        };

        var startTime = DateTime.UtcNow;

        try
        {
            // Full page load: primary document + all sub-resources
            var pageLoad = await _scenarioEngine.NavigateFullAsync(url, ct: ct);
            result.Traces.AddRange(pageLoad.AllTraces);

            // Execute keyword searches with full page loads
            string? lastUrl = url;
            foreach (var keyword in target.Keywords)
            {
                ct.ThrowIfCancellationRequested();
                var searchLoad = await _scenarioEngine.SearchFullAsync(url, keyword, lastUrl, ct);
                result.Traces.AddRange(searchLoad.AllTraces);
                lastUrl = searchLoad.PrimaryTrace.Url;
            }

            // Analyze performance (now with classification + breakdown)
            result.Metrics = _analyzer.Analyze(result.Traces, url);

            // Advanced rule evaluation with page-type-aware thresholds
            result.Alerts = _ruleEngine.EvaluateAdvanced(
                result.Metrics, result.Traces, config.Thresholds);

            // Capture evidence if enabled
            if (config.ScreenshotEnabled && _evidenceCollector.IsAvailable)
            {
                result.ScreenshotPath = await _evidenceCollector.CaptureScreenshotAsync(
                    url, config.EvidenceDirectory, ct);
            }

            // Save to history
            await _historicalStore.SaveAsync(url, result.Metrics, ct);

            result.Success = pageLoad.PrimaryTrace.ErrorMessage is null;
            result.ErrorMessage = pageLoad.PrimaryTrace.ErrorMessage;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.Error($"Monitoring failed for {url}", ex);
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    private static List<string> FilterUrls(List<string> urls, AppConfiguration config)
    {
        var filtered = urls.AsEnumerable();

        // Apply ignore patterns
        foreach (var pattern in config.IgnorePatterns)
        {
            var ext = pattern.TrimStart('*');
            filtered = filtered.Where(u => !u.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        // Apply whitelist (if configured)
        if (config.Whitelist.Count > 0)
        {
            filtered = filtered.Where(u =>
                config.Whitelist.Any(w => u.Contains(w, StringComparison.OrdinalIgnoreCase)));
        }

        return filtered.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task PersistPageVisitAsync(
        MonitoringResult result,
        Guid scenarioExecutionId,
        Guid runSessionId,
        CancellationToken ct)
    {
        try
        {
            var pageVisit = DataMapper.ToPageVisit(result, scenarioExecutionId, runSessionId);
            await _database.InsertPageVisitAsync(pageVisit, ct);

            // Network requests (batch insert for performance)
            var networkRequests = result.Traces
                .Select(t => DataMapper.ToNetworkRequest(t, pageVisit.Id))
                .ToList();
            if (networkRequests.Count > 0)
                await _database.InsertNetworkRequestsAsync(networkRequests, ct);

            // Detected issues
            foreach (var alert in result.Alerts)
            {
                var issue = DataMapper.ToDetectedIssue(alert, pageVisit.Id, runSessionId);
                await _database.InsertDetectedIssueAsync(issue, ct);
            }

            // Evidence (screenshot)
            if (result.ScreenshotPath is not null)
            {
                var evidence = DataMapper.ToScreenshotEvidence(result.ScreenshotPath, pageVisit.Id);
                await _database.InsertEvidenceItemAsync(evidence, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to persist PageVisit for {result.Url}", ex);
        }
    }

    private async Task BuildBaselineComparisonsAsync(Guid currentRunId, CancellationToken ct)
    {
        try
        {
            var sessions = await _database.GetRunSessionsAsync(limit: 2, ct: ct);
            if (sessions.Count < 2) return;

            var baselineRunId = sessions[1].Id;
            var currentVisits = await _database.GetPageVisitsByRunAsync(currentRunId, ct);
            var baselineVisits = await _database.GetPageVisitsByRunAsync(baselineRunId, ct);

            var currentByUrl = currentVisits.GroupBy(v => v.Url).ToDictionary(g => g.Key, g => g.ToList());
            var baselineByUrl = baselineVisits.GroupBy(v => v.Url).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (url, visits) in currentByUrl)
            {
                baselineByUrl.TryGetValue(url, out var baseVisits);
                var comparison = DataMapper.BuildComparison(
                    url, currentRunId, baselineRunId,
                    visits, baseVisits ?? []);
                await _database.InsertBaselineComparisonAsync(comparison, ct);
            }

            _logger.Info($"Baseline comparisons generated: {currentByUrl.Count} URLs compared");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to build baseline comparisons", ex);
        }
    }
}
