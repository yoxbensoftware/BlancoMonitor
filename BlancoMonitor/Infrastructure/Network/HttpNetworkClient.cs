using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Infrastructure.Network;

public sealed partial class HttpNetworkClient : INetworkClient
{
    private readonly HttpClient _client;
    private readonly IAppLogger _logger;
    private const int MaxSubResources = 60;
    private const int SubResourceParallelism = 6;

    public HttpNetworkClient(string userAgent, int timeoutSeconds, IAppLogger logger)
    {
        _logger = logger;

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        _client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds),
        };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        _client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    public async Task<NetworkTrace> SendAsync(string url, string method = "GET", CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        return await SendAsync(request, ct);
    }

    public async Task<NetworkTrace> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        var trace = new NetworkTrace
        {
            Url = request.RequestUri?.ToString() ?? string.Empty,
            Method = request.Method.Method,
            Timestamp = DateTime.UtcNow,
        };

        foreach (var header in request.Headers)
            trace.RequestHeaders[header.Key] = string.Join(", ", header.Value);

        var totalStopwatch = Stopwatch.StartNew();
        var ttfbStopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            ttfbStopwatch.Stop();
            trace.TimeToFirstByteMs = ttfbStopwatch.Elapsed.TotalMilliseconds;
            trace.StatusCode = (int)response.StatusCode;
            trace.ContentType = response.Content.Headers.ContentType?.ToString();

            foreach (var header in response.Headers)
                trace.ResponseHeaders[header.Key] = string.Join(", ", header.Value);

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            totalStopwatch.Stop();

            trace.ContentLength = content.LongLength;
            trace.TotalTimeMs = totalStopwatch.Elapsed.TotalMilliseconds;
            trace.ContentDownloadMs = trace.TotalTimeMs - trace.TimeToFirstByteMs;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            totalStopwatch.Stop();
            ttfbStopwatch.Stop();
            trace.TotalTimeMs = totalStopwatch.Elapsed.TotalMilliseconds;
            trace.ErrorMessage = ex.Message;
            trace.StatusCode = ex is HttpRequestException httpEx ? (int)(httpEx.StatusCode ?? 0) : 0;
        }

        return trace;
    }

    public async Task<PageLoadResult> FetchPageWithResourcesAsync(
        string url,
        string? referer = null,
        CancellationToken ct = default)
    {
        var pageLoadStopwatch = Stopwatch.StartNew();
        var result = new PageLoadResult();

        // 1. Fetch primary document
        var primaryRequest = new HttpRequestMessage(HttpMethod.Get, url);
        if (referer is not null)
            primaryRequest.Headers.Referrer = new Uri(referer);

        result.PrimaryTrace = await SendWithBodyAsync(primaryRequest, ct);
        result.PrimaryTrace.Category = RequestCategory.Html;

        string? pageHost = null;
        try { pageHost = new Uri(url).Host; } catch { }

        // 2. If HTML, parse for sub-resources
        if (result.HtmlContent is not null || IsHtmlResponse(result.PrimaryTrace))
        {
            // Re-fetch with body if we don't have content
            if (result.HtmlContent is null)
            {
                try
                {
                    using var bodyResponse = await _client.GetAsync(url, ct);
                    result.HtmlContent = await bodyResponse.Content.ReadAsStringAsync(ct);
                }
                catch { }
            }

            if (result.HtmlContent is not null)
            {
                var resourceUrls = ExtractSubResourceUrls(result.HtmlContent, url);
                var uniqueUrls = resourceUrls
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxSubResources)
                    .ToList();

                _logger.Info($"Discovered {uniqueUrls.Count} sub-resources for {url}");

                // 3. Fetch sub-resources in parallel with throttling
                var semaphore = new SemaphoreSlim(SubResourceParallelism);
                var subTasks = uniqueUrls.Select(async subUrl =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var subRequest = new HttpRequestMessage(HttpMethod.Get, subUrl);
                        subRequest.Headers.Referrer = new Uri(url);
                        var trace = await SendAsync(subRequest, ct);
                        trace.InitiatorUrl = url;
                        trace.Category = RequestClassifier.Classify(trace, pageHost);
                        trace.IsThirdParty = pageHost is not null &&
                            !new Uri(subUrl).Host.Contains(GetRootDomain(pageHost), StringComparison.OrdinalIgnoreCase);
                        return trace;
                    }
                    catch (Exception ex)
                    {
                        return new NetworkTrace
                        {
                            Url = subUrl,
                            ErrorMessage = ex.Message,
                            InitiatorUrl = url,
                            Timestamp = DateTime.UtcNow,
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var subTraces = await Task.WhenAll(subTasks);
                result.SubResourceTraces.AddRange(subTraces);
            }
        }

        pageLoadStopwatch.Stop();
        result.FullPageLoadMs = pageLoadStopwatch.Elapsed.TotalMilliseconds;

        // 4. Estimate DOM ready time (primary HTML + CSS + synchronous JS)
        var criticalResourceTime = result.SubResourceTraces
            .Where(t => t.Category is RequestCategory.Css or RequestCategory.JavaScript && !t.IsThirdParty)
            .Select(t => t.TotalTimeMs)
            .DefaultIfEmpty(0)
            .Max();
        result.DomReadyEstimateMs = result.PrimaryTrace.TotalTimeMs + criticalResourceTime;

        // 5. Detect page type
        result.DetectedPageType = PageTypeDetector.Detect(url);

        // 6. Single page-level summary log (instead of per-sub-resource logging)
        var failedCount = result.SubResourceTraces.Count(t => t.ErrorMessage is not null);
        _logger.Info($"[{result.PrimaryTrace.StatusCode}] GET {url} — TTFB={result.PrimaryTrace.TimeToFirstByteMs:F0}ms, " +
                     $"Full={result.FullPageLoadMs:F0}ms, " +
                     $"Sub={result.SubResourceTraces.Count}({failedCount} failed)");

        return result;
    }

    // ── Private helpers ─────────────────────────────────────────

    private async Task<NetworkTrace> SendWithBodyAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var trace = new NetworkTrace
        {
            Url = request.RequestUri?.ToString() ?? string.Empty,
            Method = request.Method.Method,
            Timestamp = DateTime.UtcNow,
        };

        var totalStopwatch = Stopwatch.StartNew();
        var ttfbStopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            ttfbStopwatch.Stop();
            trace.TimeToFirstByteMs = ttfbStopwatch.Elapsed.TotalMilliseconds;
            trace.StatusCode = (int)response.StatusCode;
            trace.ContentType = response.Content.Headers.ContentType?.ToString();

            foreach (var header in response.Headers)
                trace.ResponseHeaders[header.Key] = string.Join(", ", header.Value);

            var content = await response.Content.ReadAsStringAsync(ct);
            totalStopwatch.Stop();

            trace.ContentLength = content.Length;
            trace.TotalTimeMs = totalStopwatch.Elapsed.TotalMilliseconds;
            trace.ContentDownloadMs = trace.TotalTimeMs - trace.TimeToFirstByteMs;

            _logger.Info($"[{trace.StatusCode}] {trace.Method} {trace.Url} — {trace.TotalTimeMs:F0}ms (with body)");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            totalStopwatch.Stop();
            ttfbStopwatch.Stop();
            trace.TotalTimeMs = totalStopwatch.Elapsed.TotalMilliseconds;
            trace.ErrorMessage = ex.Message;
            trace.StatusCode = ex is HttpRequestException httpEx ? (int)(httpEx.StatusCode ?? 0) : 0;
            _logger.Error($"Request failed: {trace.Url}", ex);
        }

        return trace;
    }

    /// <summary>
    /// Extracts CSS, JS, image, and font URLs from HTML content using regex patterns.
    /// Resolves relative URLs to absolute using the page URL as base.
    /// </summary>
    private static List<string> ExtractSubResourceUrls(string html, string pageUrl)
    {
        var urls = new List<string>();
        Uri baseUri;
        try { baseUri = new Uri(pageUrl); }
        catch { return urls; }

        // <link href="..."> (CSS, fonts, icons)
        foreach (Match m in LinkHrefRegex().Matches(html))
            TryAddUrl(m.Groups[1].Value, baseUri, urls);

        // <script src="...">
        foreach (Match m in ScriptSrcRegex().Matches(html))
            TryAddUrl(m.Groups[1].Value, baseUri, urls);

        // <img src="...">
        foreach (Match m in ImgSrcRegex().Matches(html))
            TryAddUrl(m.Groups[1].Value, baseUri, urls);

        // url(...) in inline styles
        foreach (Match m in CssUrlRegex().Matches(html))
            TryAddUrl(m.Groups[1].Value, baseUri, urls);

        // <source src="..."> (video/picture)
        foreach (Match m in SourceSrcRegex().Matches(html))
            TryAddUrl(m.Groups[1].Value, baseUri, urls);

        return urls;
    }

    private static void TryAddUrl(string raw, Uri baseUri, List<string> urls)
    {
        raw = raw.Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("data:") || raw.StartsWith("blob:"))
            return;

        try
        {
            var absolute = new Uri(baseUri, raw).AbsoluteUri;
            if (absolute.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                urls.Add(absolute);
        }
        catch { }
    }

    private static bool IsHtmlResponse(NetworkTrace trace) =>
        trace.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;

    private static string GetRootDomain(string host)
    {
        var parts = host.Split('.');
        return parts.Length >= 2 ? $"{parts[^2]}.{parts[^1]}" : host;
    }

    // Compiled regex patterns for HTML parsing
    [GeneratedRegex("""<link[^>]+href\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LinkHrefRegex();

    [GeneratedRegex("""<script[^>]+src\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ScriptSrcRegex();

    [GeneratedRegex("""<img[^>]+src\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ImgSrcRegex();

    [GeneratedRegex("""url\(\s*['"]?([^'")]+)['"]?\s*\)""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CssUrlRegex();

    [GeneratedRegex("""<source[^>]+src\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SourceSrcRegex();

    public void Dispose() => _client.Dispose();
}
