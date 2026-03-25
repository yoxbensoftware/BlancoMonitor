namespace BlancoMonitor.Domain.Entities;

public sealed class MonitorTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public int CheckIntervalSeconds { get; set; } = 60;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
