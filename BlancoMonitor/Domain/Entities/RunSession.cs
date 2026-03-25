using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Top-level container for a single monitoring run.
/// One RunSession is created each time the user clicks "Start".
/// All ScenarioExecutions, PageVisits, and DetectedIssues belong to a session.
/// </summary>
public sealed class RunSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public MonitorStatus Status { get; set; } = MonitorStatus.Idle;
    public int TotalUrls { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public double TotalDurationMs { get; set; }
    public string? ReportPath { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public List<ScenarioExecution> Executions { get; set; } = [];
}
