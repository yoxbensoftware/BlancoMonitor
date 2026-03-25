using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

public sealed class NetworkTrace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int StatusCode { get; set; }
    public Dictionary<string, string> RequestHeaders { get; set; } = [];
    public Dictionary<string, string> ResponseHeaders { get; set; } = [];
    public double TimeToFirstByteMs { get; set; }
    public double TotalTimeMs { get; set; }
    public double ContentDownloadMs { get; set; }
    public string? ContentType { get; set; }
    public long ContentLength { get; set; }
    public List<string> RedirectChain { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public RequestCategory Category { get; set; } = RequestCategory.Other;
    public string? InitiatorUrl { get; set; }
    public bool IsThirdParty { get; set; }
}
