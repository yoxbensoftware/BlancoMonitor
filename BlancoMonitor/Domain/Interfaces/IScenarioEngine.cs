using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

public interface IScenarioEngine
{
    Task<List<NetworkTrace>> ExecuteAsync(
        ScenarioDefinition scenario,
        CancellationToken ct = default);

    Task<NetworkTrace> NavigateAsync(string url, CancellationToken ct = default);

    Task<NetworkTrace> SearchAsync(
        string baseUrl,
        string keyword,
        CancellationToken ct = default);

    /// <summary>
    /// Full page load: fetches the URL + all sub-resources, classifies requests,
    /// simulates realistic user timing (referer, cookies, delays).
    /// </summary>
    Task<PageLoadResult> NavigateFullAsync(
        string url,
        string? referer = null,
        CancellationToken ct = default);

    /// <summary>
    /// Full search simulation: navigates to the search URL, captures the result page
    /// and all its sub-resources.
    /// </summary>
    Task<PageLoadResult> SearchFullAsync(
        string baseUrl,
        string keyword,
        string? referer = null,
        CancellationToken ct = default);
}
