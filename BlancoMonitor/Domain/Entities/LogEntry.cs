namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Structured log entry persisted to SQLite for searchable, queryable logging.
/// Each log entry captures context (source, run, url) for filtering and analysis.
/// </summary>
public sealed class LogEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Exception { get; set; }
    public Guid? RunSessionId { get; set; }
    public string? Url { get; set; }
    public double? ElapsedMs { get; set; }
    public Dictionary<string, string> Properties { get; set; } = [];
}
