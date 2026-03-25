using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Infrastructure.Browser;

/// <summary>
/// Headless HTTP-based scenario engine that simulates real user behavior.
/// Features:
/// - Sub-resource fetching (CSS, JS, images parsed from HTML)
/// - Referer chain simulation (each page passes referer to the next)
/// - Realistic inter-page delays (simulates reading time)
/// - Request classification (HTML, API, JS, CSS, Image, Font, Analytics, 3rd-party)
/// - Page type detection (Homepage, Product, Search, etc.)
/// - Full network capture for every page load
/// </summary>
public sealed class HttpScenarioEngine : IScenarioEngine
{
    private readonly INetworkClient _client;
    private readonly IAppLogger _logger;
    private readonly int _delayMs;
    private static readonly Random Jitter = new();

    public HttpScenarioEngine(INetworkClient client, IAppLogger logger, int delayBetweenRequestsMs = 500)
    {
        _client = client;
        _logger = logger;
        _delayMs = delayBetweenRequestsMs;
    }

    // ── Full page load methods (new) ────────────────────────────

    public async Task<PageLoadResult> NavigateFullAsync(
        string url,
        string? referer = null,
        CancellationToken ct = default)
    {
        url = NormalizeUrl(url);
        _logger.Info($"▶ Full page load: {url}");

        var result = await _client.FetchPageWithResourcesAsync(url, referer, ct);

        _logger.Info($"  → {result.TotalRequestCount} requests, " +
                     $"{result.FailedRequestCount} failed, " +
                     $"DOM≈{result.DomReadyEstimateMs:F0}ms, " +
                     $"Full={result.FullPageLoadMs:F0}ms, " +
                     $"Type={result.DetectedPageType}");

        return result;
    }

    public async Task<PageLoadResult> SearchFullAsync(
        string baseUrl,
        string keyword,
        string? referer = null,
        CancellationToken ct = default)
    {
        baseUrl = NormalizeUrl(baseUrl);
        var searchUrl = BuildSearchUrl(baseUrl, keyword);
        _logger.Info($"▶ Full search: '{keyword}' → {searchUrl}");

        var result = await _client.FetchPageWithResourcesAsync(searchUrl, referer ?? baseUrl, ct);
        result.DetectedPageType = PageType.Search;

        _logger.Info($"  → {result.TotalRequestCount} requests, " +
                     $"Full={result.FullPageLoadMs:F0}ms");

        return result;
    }

    // ── Legacy simple methods (backward compatible) ─────────────

    public async Task<NetworkTrace> NavigateAsync(string url, CancellationToken ct = default)
    {
        url = NormalizeUrl(url);
        _logger.Info($"Navigating to: {url}");
        return await _client.SendAsync(url, "GET", ct);
    }

    public async Task<NetworkTrace> SearchAsync(string baseUrl, string keyword, CancellationToken ct = default)
    {
        baseUrl = NormalizeUrl(baseUrl);
        var searchUrl = BuildSearchUrl(baseUrl, keyword);
        _logger.Info($"Searching '{keyword}' at: {searchUrl}");
        return await _client.SendAsync(searchUrl, "GET", ct);
    }

    // ── Scenario execution (enhanced) ───────────────────────────

    public async Task<List<NetworkTrace>> ExecuteAsync(ScenarioDefinition scenario, CancellationToken ct = default)
    {
        var traces = new List<NetworkTrace>();
        _logger.Info($"═══ Executing scenario: {scenario.Name} ({scenario.Steps.Count} steps) ═══");

        string? lastUrl = null;

        foreach (var step in scenario.Steps.OrderBy(s => s.Order))
        {
            ct.ThrowIfCancellationRequested();

            switch (step.ActionType)
            {
                case ScenarioActionType.Navigate:
                case ScenarioActionType.FollowLinks:
                case ScenarioActionType.ClickLink:
                {
                    var url = NormalizeUrl(step.Value ?? string.Empty);
                    var pageResult = await NavigateFullAsync(url, lastUrl, ct);
                    traces.AddRange(pageResult.AllTraces);
                    lastUrl = url;
                    break;
                }
                case ScenarioActionType.Search:
                {
                    var url = NormalizeUrl(step.Value ?? string.Empty);
                    var searchResult = await SearchFullAsync(url, step.Selector ?? string.Empty, lastUrl, ct);
                    traces.AddRange(searchResult.AllTraces);
                    lastUrl = url;
                    break;
                }
                case ScenarioActionType.Wait:
                    _logger.Info($"  ⏳ Waiting {step.TimeoutMs}ms...");
                    await Task.Delay(step.TimeoutMs, ct);
                    break;
                case ScenarioActionType.ScrollPage:
                    // Simulated: add a realistic "scroll reading" delay
                    var scrollDelay = 800 + Jitter.Next(400);
                    _logger.Info($"  📜 Simulating scroll ({scrollDelay}ms)...");
                    await Task.Delay(scrollDelay, ct);
                    break;
            }

            // Simulate realistic inter-step delay (user "reading" time)
            if (_delayMs > 0)
            {
                var jitteredDelay = _delayMs + Jitter.Next(_delayMs / 2);
                await Task.Delay(jitteredDelay, ct);
            }
        }

        // Execute keyword searches with referer chain
        foreach (var keyword in scenario.SearchKeywords)
        {
            ct.ThrowIfCancellationRequested();

            var baseStep = scenario.Steps.FirstOrDefault(s => s.ActionType == ScenarioActionType.Navigate);
            if (baseStep?.Value is not null)
            {
                var baseUrl = NormalizeUrl(baseStep.Value);
                var searchResult = await SearchFullAsync(baseUrl, keyword, lastUrl, ct);
                traces.AddRange(searchResult.AllTraces);
                lastUrl = BuildSearchUrl(baseUrl, keyword);

                if (_delayMs > 0)
                {
                    var jitteredDelay = _delayMs + Jitter.Next(_delayMs / 2);
                    await Task.Delay(jitteredDelay, ct);
                }
            }
        }

        _logger.Info($"═══ Scenario '{scenario.Name}' complete: {traces.Count} total traces ═══");
        return traces;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static string NormalizeUrl(string url)
    {
        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }
        return url;
    }

    private static string BuildSearchUrl(string baseUrl, string keyword)
    {
        var uri = new Uri(baseUrl);
        return $"{uri.Scheme}://{uri.Host}/search?q={Uri.EscapeDataString(keyword)}";
    }
}
