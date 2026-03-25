namespace BlancoMonitor.Domain.Entities;

public sealed class MonitoringResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TargetId { get; set; }
    public string Url { get; set; } = string.Empty;
    public PerformanceMetric? Metrics { get; set; }
    public List<NetworkTrace> Traces { get; set; } = [];
    public List<Alert> Alerts { get; set; } = [];
    public string? ScreenshotPath { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
