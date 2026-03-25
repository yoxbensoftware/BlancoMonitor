using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

public interface INetworkClient : IDisposable
{
    Task<NetworkTrace> SendAsync(string url, string method = "GET", CancellationToken ct = default);
    Task<NetworkTrace> SendAsync(HttpRequestMessage request, CancellationToken ct = default);

    /// <summary>
    /// Fetches the primary URL, parses the HTML response to discover sub-resources
    /// (CSS, JS, images, fonts, APIs), then fetches them in parallel.
    /// Returns the complete page load picture.
    /// </summary>
    Task<PageLoadResult> FetchPageWithResourcesAsync(
        string url,
        string? referer = null,
        CancellationToken ct = default);
}
