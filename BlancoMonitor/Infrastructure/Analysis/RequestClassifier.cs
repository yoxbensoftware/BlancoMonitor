using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Infrastructure.Analysis;

/// <summary>
/// Classifies network requests by content type and URL pattern.
/// Determines if a request is HTML, API, JS, CSS, Image, Font, Analytics, or third-party.
/// </summary>
public static class RequestClassifier
{
    private static readonly HashSet<string> AnalyticsDomains =
    [
        "google-analytics.com", "googletagmanager.com", "analytics.google.com",
        "facebook.net", "connect.facebook.net", "doubleclick.net",
        "hotjar.com", "clarity.ms", "newrelic.com", "segment.io",
        "mixpanel.com", "amplitude.com", "heapanalytics.com",
        "fullstory.com", "mouseflow.com", "crazyegg.com",
        "optimizely.com", "adobedtm.com", "omtrdc.net",
        "bat.bing.com", "ads.linkedin.com", "snap.licdn.com",
        "sentry.io", "bugsnag.com", "datadoghq.com",
    ];

    private static readonly HashSet<string> FontExtensions =
        [".woff", ".woff2", ".ttf", ".otf", ".eot"];

    private static readonly HashSet<string> ImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".ico", ".avif", ".bmp"];

    private static readonly HashSet<string> ApiPatterns =
        ["/api/", "/graphql", "/rest/", "/v1/", "/v2/", "/v3/", "/.well-known/"];

    public static RequestCategory Classify(NetworkTrace trace, string? pageHost = null)
    {
        var url = trace.Url.ToLowerInvariant();
        var contentType = trace.ContentType?.ToLowerInvariant() ?? string.Empty;

        // 1. Third-party analytics
        if (IsAnalyticsDomain(url))
            return RequestCategory.Analytics;

        // 2. Third-party detection
        if (pageHost is not null && IsThirdParty(url, pageHost))
        {
            trace.IsThirdParty = true;
            // Still classify by type, but mark as third-party
        }

        // 3. Content-type based classification
        if (contentType.Contains("text/html") || contentType.Contains("application/xhtml"))
            return RequestCategory.Html;

        if (contentType.Contains("application/json") || contentType.Contains("application/xml") ||
            contentType.Contains("text/xml") || contentType.Contains("application/graphql"))
            return RequestCategory.Api;

        if (contentType.Contains("javascript") || contentType.Contains("ecmascript"))
            return RequestCategory.JavaScript;

        if (contentType.Contains("text/css"))
            return RequestCategory.Css;

        if (contentType.Contains("image/") || contentType.Contains("image/svg"))
            return RequestCategory.Image;

        if (contentType.Contains("font/") || contentType.Contains("application/font") ||
            contentType.Contains("application/x-font"))
            return RequestCategory.Font;

        // 4. URL-pattern based fallback
        var path = GetUrlPath(url);

        if (FontExtensions.Any(ext => path.EndsWith(ext)))
            return RequestCategory.Font;

        if (ImageExtensions.Any(ext => path.EndsWith(ext)))
            return RequestCategory.Image;

        if (path.EndsWith(".js") || path.EndsWith(".mjs"))
            return RequestCategory.JavaScript;

        if (path.EndsWith(".css"))
            return RequestCategory.Css;

        if (ApiPatterns.Any(p => path.Contains(p)))
            return RequestCategory.Api;

        if (path.EndsWith(".html") || path.EndsWith(".htm") || path == "/" || !path.Contains('.'))
            return RequestCategory.Html;

        return RequestCategory.Other;
    }

    public static void ClassifyAll(IEnumerable<NetworkTrace> traces, string? pageHost = null)
    {
        foreach (var trace in traces)
        {
            trace.Category = Classify(trace, pageHost);
            if (pageHost is not null)
                trace.IsThirdParty = IsThirdParty(trace.Url, pageHost);
        }
    }

    private static bool IsAnalyticsDomain(string url)
    {
        try
        {
            var host = new Uri(url).Host.ToLowerInvariant();
            return AnalyticsDomains.Any(d => host.Contains(d));
        }
        catch { return false; }
    }

    private static bool IsThirdParty(string url, string pageHost)
    {
        try
        {
            var requestHost = new Uri(url).Host.ToLowerInvariant();
            var baseHost = pageHost.ToLowerInvariant();
            // Extract root domain (e.g., "cdn.blanco.de" → "blanco.de")
            var requestRoot = GetRootDomain(requestHost);
            var pageRoot = GetRootDomain(baseHost);
            return !string.Equals(requestRoot, pageRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static string GetRootDomain(string host)
    {
        var parts = host.Split('.');
        return parts.Length >= 2
            ? $"{parts[^2]}.{parts[^1]}"
            : host;
    }

    private static string GetUrlPath(string url)
    {
        try { return new Uri(url).AbsolutePath.ToLowerInvariant(); }
        catch { return url; }
    }
}
