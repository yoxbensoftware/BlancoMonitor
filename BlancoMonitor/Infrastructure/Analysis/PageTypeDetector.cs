using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Infrastructure.Analysis;

/// <summary>
/// Detects the type of a web page based on its URL path patterns.
/// Used to apply different performance thresholds per page type.
/// </summary>
public static class PageTypeDetector
{
    private static readonly string[] HomepagePaths =
        ["/", "/index", "/index.html", "/home", "/default"];

    private static readonly string[] SearchPaths =
        ["/search", "/arama", "/suche", "/find", "/results", "?q=", "?s=", "?search=", "?query="];

    private static readonly string[] ProductPaths =
        ["/product", "/item", "/detail", "/urun", "/p/", "/dp/", "/pd/", "/sku/"];

    private static readonly string[] CategoryPaths =
        ["/category", "/kategori", "/collection", "/catalog", "/shop/", "/c/", "/dept/"];

    private static readonly string[] CheckoutPaths =
        ["/checkout", "/cart", "/basket", "/payment", "/order", "/sepet", "/odeme"];

    private static readonly string[] ApiPaths =
        ["/api/", "/graphql", "/rest/", "/v1/", "/v2/", "/v3/", "/.well-known/"];

    private static readonly string[] StaticPaths =
        ["/static/", "/assets/", "/media/", "/cdn/", "/dist/"];

    public static PageType Detect(string url)
    {
        string path;
        string query;
        try
        {
            var uri = new Uri(url);
            path = uri.AbsolutePath.ToLowerInvariant().TrimEnd('/');
            query = uri.Query.ToLowerInvariant();
        }
        catch
        {
            return PageType.Unknown;
        }

        var fullPath = path + query;

        // Order matters: more specific patterns first
        if (CheckoutPaths.Any(p => fullPath.Contains(p)))
            return PageType.Checkout;

        if (SearchPaths.Any(p => fullPath.Contains(p)))
            return PageType.Search;

        if (ProductPaths.Any(p => fullPath.Contains(p)))
            return PageType.Product;

        if (CategoryPaths.Any(p => fullPath.Contains(p)))
            return PageType.Category;

        if (ApiPaths.Any(p => fullPath.Contains(p)))
            return PageType.Api;

        if (StaticPaths.Any(p => fullPath.Contains(p)))
            return PageType.Static;

        if (HomepagePaths.Any(p => path == p || path == string.Empty))
            return PageType.Homepage;

        return PageType.Unknown;
    }
}
