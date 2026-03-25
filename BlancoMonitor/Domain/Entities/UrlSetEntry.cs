namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Individual URL within a UrlSet.
/// Tracks whether the URL was manually configured or auto-discovered via sitemap.
/// </summary>
public sealed class UrlSetEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UrlSetId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsDiscovered { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UrlSet? UrlSet { get; set; }
}
