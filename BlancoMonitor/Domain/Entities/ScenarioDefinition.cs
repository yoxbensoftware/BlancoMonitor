using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

public sealed class ScenarioDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public List<ScenarioStep> Steps { get; set; } = [];
    public List<string> SearchKeywords { get; set; } = [];
}

public sealed class ScenarioStep
{
    public int Order { get; set; }
    public ScenarioActionType ActionType { get; set; }
    public string? Selector { get; set; }
    public string? Value { get; set; }
    public int TimeoutMs { get; set; } = 30_000;
}
