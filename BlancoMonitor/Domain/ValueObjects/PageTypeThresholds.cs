using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.ValueObjects;

/// <summary>
/// Different page types have different acceptable performance thresholds.
/// A homepage should load faster than a search results page, for example.
/// </summary>
public sealed record PageTypeThresholds
{
    public PageType PageType { get; init; }
    public double TtfbWarningMs { get; init; }
    public double TtfbCriticalMs { get; init; }
    public double TotalWarningMs { get; init; }
    public double TotalCriticalMs { get; init; }
    public double ApiWarningMs { get; init; }
    public double ApiCriticalMs { get; init; }
    public int MaxAcceptableRequestCount { get; init; }

    public static IReadOnlyDictionary<PageType, PageTypeThresholds> Defaults { get; } =
        new Dictionary<PageType, PageTypeThresholds>
        {
            [PageType.Homepage] = new()
            {
                PageType = PageType.Homepage,
                TtfbWarningMs = 800, TtfbCriticalMs = 2000,
                TotalWarningMs = 2500, TotalCriticalMs = 6000,
                ApiWarningMs = 500, ApiCriticalMs = 2000,
                MaxAcceptableRequestCount = 80,
            },
            [PageType.Product] = new()
            {
                PageType = PageType.Product,
                TtfbWarningMs = 1000, TtfbCriticalMs = 3000,
                TotalWarningMs = 3000, TotalCriticalMs = 8000,
                ApiWarningMs = 800, ApiCriticalMs = 3000,
                MaxAcceptableRequestCount = 100,
            },
            [PageType.Search] = new()
            {
                PageType = PageType.Search,
                TtfbWarningMs = 1200, TtfbCriticalMs = 4000,
                TotalWarningMs = 4000, TotalCriticalMs = 10000,
                ApiWarningMs = 1000, ApiCriticalMs = 4000,
                MaxAcceptableRequestCount = 60,
            },
            [PageType.Category] = new()
            {
                PageType = PageType.Category,
                TtfbWarningMs = 1000, TtfbCriticalMs = 3000,
                TotalWarningMs = 3500, TotalCriticalMs = 9000,
                ApiWarningMs = 800, ApiCriticalMs = 3000,
                MaxAcceptableRequestCount = 90,
            },
            [PageType.Checkout] = new()
            {
                PageType = PageType.Checkout,
                TtfbWarningMs = 800, TtfbCriticalMs = 2000,
                TotalWarningMs = 2000, TotalCriticalMs = 5000,
                ApiWarningMs = 500, ApiCriticalMs = 1500,
                MaxAcceptableRequestCount = 50,
            },
            [PageType.Api] = new()
            {
                PageType = PageType.Api,
                TtfbWarningMs = 300, TtfbCriticalMs = 1000,
                TotalWarningMs = 500, TotalCriticalMs = 2000,
                ApiWarningMs = 300, ApiCriticalMs = 1000,
                MaxAcceptableRequestCount = 1,
            },
            [PageType.Static] = new()
            {
                PageType = PageType.Static,
                TtfbWarningMs = 500, TtfbCriticalMs = 1500,
                TotalWarningMs = 1500, TotalCriticalMs = 4000,
                ApiWarningMs = 1000, ApiCriticalMs = 3000,
                MaxAcceptableRequestCount = 30,
            },
            [PageType.Unknown] = new()
            {
                PageType = PageType.Unknown,
                TtfbWarningMs = 1000, TtfbCriticalMs = 3000,
                TotalWarningMs = 3000, TotalCriticalMs = 10000,
                ApiWarningMs = 800, ApiCriticalMs = 3000,
                MaxAcceptableRequestCount = 100,
            },
        };
}
