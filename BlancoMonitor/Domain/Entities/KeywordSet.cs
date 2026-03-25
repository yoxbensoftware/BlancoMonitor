namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// A named set of keywords associated with a UrlSet.
/// Keywords are stored as comma-separated text for simplicity.
/// Used by the ScenarioEngine to simulate product searches.
/// </summary>
public sealed class KeywordSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UrlSetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeywordsCsv { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UrlSet? UrlSet { get; set; }

    /// <summary>Returns individual keywords parsed from CSV.</summary>
    public IReadOnlyList<string> GetKeywords() =>
        KeywordsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
