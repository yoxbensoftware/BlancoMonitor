namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Reusable scenario template that defines a monitoring workflow.
/// Steps are serialized as JSON for flexible storage.
/// Links to a UrlSet to know which URLs to target.
/// </summary>
public sealed class Scenario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid UrlSetId { get; set; }
    public string StepsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public UrlSet? UrlSet { get; set; }
    public List<ScenarioExecution> Executions { get; set; } = [];
}
