using System.Xml.Linq;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Discovery;

public sealed class SitemapDiscoveryEngine : IDiscoveryEngine
{
    private readonly INetworkClient _client;
    private readonly IAppLogger _logger;

    public SitemapDiscoveryEngine(INetworkClient client, IAppLogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<string>> DiscoverUrlsAsync(string baseUrl, CancellationToken ct = default)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        baseUrl = NormalizeUrl(baseUrl);
        var uri = new Uri(baseUrl);
        var baseHost = uri.Host;

        // 1. Try robots.txt for sitemap references
        var robotsUrl = $"{uri.Scheme}://{baseHost}/robots.txt";
        _logger.Info($"Checking robots.txt: {robotsUrl}");

        var robotsTrace = await _client.SendAsync(robotsUrl, "GET", ct);
        if (robotsTrace.StatusCode == 200 && robotsTrace.ContentLength > 0)
        {
            // Re-fetch to get content (trace doesn't store body)
            using var httpClient = new HttpClient();
            try
            {
                var robotsContent = await httpClient.GetStringAsync(robotsUrl, ct);
                foreach (var line in robotsContent.Split('\n'))
                {
                    if (line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                    {
                        var sitemapUrl = line["Sitemap:".Length..].Trim();
                        var sitemapUrls = await ParseSitemapAsync(sitemapUrl, ct);
                        foreach (var u in sitemapUrls)
                            urls.Add(u);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to parse robots.txt: {ex.Message}");
            }
        }

        // 2. Try common sitemap locations
        if (urls.Count == 0)
        {
            var commonSitemaps = new[]
            {
                $"{uri.Scheme}://{baseHost}/sitemap.xml",
                $"{uri.Scheme}://{baseHost}/sitemap_index.xml",
            };

            foreach (var sitemapUrl in commonSitemaps)
            {
                var sitemapUrls = await ParseSitemapAsync(sitemapUrl, ct);
                foreach (var u in sitemapUrls)
                    urls.Add(u);

                if (urls.Count > 0)
                    break;
            }
        }

        // 3. Always include the base URL
        urls.Add(baseUrl);

        _logger.Info($"Discovered {urls.Count} URLs from {baseUrl}");
        return [.. urls];
    }

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

    public async Task<List<string>> ParseSitemapAsync(string sitemapUrl, CancellationToken ct = default)
    {
        var urls = new List<string>();

        try
        {
            _logger.Info($"Parsing sitemap: {sitemapUrl}");
            using var httpClient = new HttpClient();
            var xml = await httpClient.GetStringAsync(sitemapUrl, ct);
            var doc = XDocument.Parse(xml);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Check if this is a sitemap index
            var sitemapElements = doc.Descendants(ns + "sitemap").Select(e => e.Element(ns + "loc")?.Value).Where(u => u is not null);
            foreach (var childSitemap in sitemapElements)
            {
                var childUrls = await ParseSitemapAsync(childSitemap!, ct);
                urls.AddRange(childUrls);
            }

            // Parse URL entries
            var urlElements = doc.Descendants(ns + "url").Select(e => e.Element(ns + "loc")?.Value).Where(u => u is not null);
            urls.AddRange(urlElements!);

            _logger.Info($"Sitemap '{sitemapUrl}' yielded {urls.Count} URLs");
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to parse sitemap '{sitemapUrl}': {ex.Message}");
        }

        return urls;
    }
}
