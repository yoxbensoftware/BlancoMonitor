using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Application.Dto;

public sealed class MonitoringSummary
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan TotalDuration => CompletedAt - StartedAt;
    public int TotalUrlsChecked { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public string? ReportPath { get; set; }
    public List<MonitoringResult> Results { get; set; } = [];
    public MonitorStatus Status { get; set; }
}

public sealed class MonitoringProgress
{
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }
    public string CurrentUrl { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public double PercentComplete => TotalCount > 0 ? (double)CurrentIndex / TotalCount * 100 : 0;
}
