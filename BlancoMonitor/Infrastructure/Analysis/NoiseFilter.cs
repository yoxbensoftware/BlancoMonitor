using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Infrastructure.Analysis;

/// <summary>
/// Filters out "noise" requests that should not affect performance scoring.
/// Analytics trackers, fonts, tracking pixels, and beacon calls are noise.
/// </summary>
public static class NoiseFilter
{
    private static readonly HashSet<string> NoisePaths =
    [
        "/collect", "/pixel", "/beacon", "/track", "/event",
        "/pageview", "/impression", "/log", "/__/", "/analytics",
        "/tag/", "/gtag/", "/gtm.js", "/fbevents.js",
    ];

    private static readonly HashSet<RequestCategory> NoiseCategories =
    [
        RequestCategory.Analytics,
        RequestCategory.Font,
    ];

    /// <summary>
    /// Returns true if this trace is considered "noise" and should be
    /// excluded from performance calculations and issue detection.
    /// </summary>
    public static bool IsNoise(NetworkTrace trace)
    {
        // Analytics category is always noise
        if (NoiseCategories.Contains(trace.Category))
            return true;

        // Tracking pixels (tiny images from third-party domains)
        if (trace.Category == RequestCategory.Image && trace.IsThirdParty && trace.ContentLength < 1024)
            return true;

        // Known noise URL patterns
        var path = GetPath(trace.Url);
        if (NoisePaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Prefetch/preload hints that aren't user-visible
        if (trace.RequestHeaders.TryGetValue("Purpose", out var purpose) &&
            purpose.Contains("prefetch", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Partitions traces into signal (meaningful) and noise (ignorable).
    /// </summary>
    public static (List<NetworkTrace> Signal, List<NetworkTrace> Noise) Partition(
        IEnumerable<NetworkTrace> traces)
    {
        var signal = new List<NetworkTrace>();
        var noise = new List<NetworkTrace>();

        foreach (var trace in traces)
        {
            if (IsNoise(trace))
                noise.Add(trace);
            else
                signal.Add(trace);
        }

        return (signal, noise);
    }

    private static string GetPath(string url)
    {
        try { return new Uri(url).PathAndQuery.ToLowerInvariant(); }
        catch { return url.ToLowerInvariant(); }
    }
}
