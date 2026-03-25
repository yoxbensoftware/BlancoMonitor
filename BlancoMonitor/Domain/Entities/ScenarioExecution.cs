using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Tracks one execution of a Scenario within a RunSession.
/// A single RunSession may execute multiple scenarios (one per UrlSet).
/// Contains the resulting PageVisits.
/// </summary>
public sealed class ScenarioExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RunSessionId { get; set; }
    public Guid? ScenarioId { get; set; }
    public Guid UrlSetId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public double DurationMs { get; set; }
    public MonitorStatus Status { get; set; } = MonitorStatus.Running;
    public int TotalPages { get; set; }
    public int PagesCompleted { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public RunSession? RunSession { get; set; }
    public Scenario? Scenario { get; set; }
    public UrlSet? UrlSet { get; set; }
    public List<PageVisit> PageVisits { get; set; } = [];
}
