using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Infrastructure.Analysis;

/// <summary>
/// Groups repeated alerts by a computed key (metric + severity + URL path pattern).
/// Determines confidence level based on occurrence count and prior run history.
/// </summary>
public static class IssueGrouper
{
    /// <summary>
    /// Assigns GroupKey to each alert, merges duplicates, and sets confidence levels.
    /// </summary>
    public static List<Alert> GroupAndDeduplicate(
        List<Alert> currentAlerts,
        IReadOnlyList<Alert>? priorAlerts = null)
    {
        // 1. Assign group keys
        foreach (var alert in currentAlerts)
            alert.GroupKey = ComputeGroupKey(alert);

        // 2. Group by key
        var grouped = currentAlerts
            .GroupBy(a => a.GroupKey)
            .Select(g =>
            {
                var representative = g.OrderByDescending(a => a.Severity).First();
                representative.OccurrenceCount = g.Count();

                // 3. Determine confidence based on occurrences in this run
                representative.Confidence = representative.OccurrenceCount switch
                {
                    >= 3 => ConfidenceLevel.Confirmed,
                    _ => ConfidenceLevel.Suspected,
                };

                // 4. Elevate to Persistent if the same issue appeared in prior runs
                if (priorAlerts is not null && priorAlerts.Any(p => p.GroupKey == representative.GroupKey))
                {
                    representative.Confidence = ConfidenceLevel.Persistent;

                    // Persistent issues that were only Warning get promoted to stay visible
                    if (representative.Severity == Severity.Notice)
                        representative.Severity = Severity.Warning;
                }

                // 5. Update message to reflect grouping
                if (representative.OccurrenceCount > 1)
                {
                    representative.Message =
                        $"[×{representative.OccurrenceCount}] {representative.Message}";
                }

                return representative;
            })
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.Confidence)
            .ThenByDescending(a => a.OccurrenceCount)
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Computes a stable key for grouping identical issues across URLs.
    /// Format: "MetricName|Severity|UrlPathPattern"
    /// </summary>
    private static string ComputeGroupKey(Alert alert)
    {
        var pathPattern = NormalizeUrlPath(alert.Url);
        return $"{alert.MetricName}|{alert.Severity}|{pathPattern}";
    }

    /// <summary>
    /// Normalizes URL path to a pattern (removes IDs and query strings)
    /// so that /product/123 and /product/456 group together.
    /// </summary>
    private static string NormalizeUrlPath(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var normalized = segments.Select(s =>
                // Replace numeric segments with placeholder
                long.TryParse(s, out _) || Guid.TryParse(s, out _) ? "{id}" : s.ToLowerInvariant()
            );
            return $"{uri.Host}/{string.Join("/", normalized)}";
        }
        catch
        {
            return url.ToLowerInvariant();
        }
    }
}
