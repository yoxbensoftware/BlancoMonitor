namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Individual HTTP request captured during a page visit.
/// Stores timing breakdown, headers (as JSON), and redirect chains.
/// Persistence version of the runtime NetworkTrace model.
/// </summary>
public sealed class NetworkRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PageVisitId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int StatusCode { get; set; }
    public double TimeToFirstByteMs { get; set; }
    public double TotalTimeMs { get; set; }
    public double ContentDownloadMs { get; set; }
    public string? ContentType { get; set; }
    public long ContentLength { get; set; }
    public string? RequestHeadersJson { get; set; }
    public string? ResponseHeadersJson { get; set; }
    public string? RedirectChainJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public PageVisit? PageVisit { get; set; }
}
