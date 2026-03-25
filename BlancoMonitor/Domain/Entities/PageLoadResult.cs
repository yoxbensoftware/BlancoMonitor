using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Full page load result including the primary document request
/// and all discovered sub-resources (CSS, JS, images, APIs, fonts).
/// Simulates what a real browser would fetch when loading a page.
/// </summary>
public sealed class PageLoadResult
{
    public NetworkTrace PrimaryTrace { get; set; } = new();
    public List<NetworkTrace> SubResourceTraces { get; set; } = [];
    public PageType DetectedPageType { get; set; } = PageType.Unknown;
    public double FullPageLoadMs { get; set; }
    public double DomReadyEstimateMs { get; set; }
    public string? HtmlContent { get; set; }

    public IReadOnlyList<NetworkTrace> AllTraces
    {
        get
        {
            var all = new List<NetworkTrace>(SubResourceTraces.Count + 1) { PrimaryTrace };
            all.AddRange(SubResourceTraces);
            return all;
        }
    }

    public int TotalRequestCount => 1 + SubResourceTraces.Count;
    public int FailedRequestCount => AllTraces.Count(t => t.ErrorMessage is not null || t.StatusCode >= 400);
}
