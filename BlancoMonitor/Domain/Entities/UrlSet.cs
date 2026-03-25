namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Named group of URLs to monitor. Replaces the flat MonitorTarget list
/// with a structured, database-backed collection.
/// Each UrlSet owns UrlSetEntries (individual URLs) and KeywordSets.
/// </summary>
public sealed class UrlSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CheckIntervalSeconds { get; set; } = 60;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<UrlSetEntry> Entries { get; set; } = [];
    public List<KeywordSet> KeywordSets { get; set; } = [];
    public List<Scenario> Scenarios { get; set; } = [];
}
