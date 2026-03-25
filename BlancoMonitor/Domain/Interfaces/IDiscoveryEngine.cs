namespace BlancoMonitor.Domain.Interfaces;

public interface IDiscoveryEngine
{
    Task<List<string>> DiscoverUrlsAsync(string baseUrl, CancellationToken ct = default);
    Task<List<string>> ParseSitemapAsync(string sitemapUrl, CancellationToken ct = default);
}
