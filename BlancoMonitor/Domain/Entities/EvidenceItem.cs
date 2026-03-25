using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// A piece of evidence captured during monitoring (screenshot, response body, etc.).
/// Linked to a PageVisit. The actual file is stored on disk; this row tracks metadata.
/// </summary>
public sealed class EvidenceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PageVisitId { get; set; }
    public EvidenceType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PageVisit? PageVisit { get; set; }
}
