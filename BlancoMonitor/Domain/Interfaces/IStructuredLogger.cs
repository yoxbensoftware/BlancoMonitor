using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Extended logger with structured context, scoped run tracking, and SQLite persistence.
/// Superset of IAppLogger — all existing consumers continue to work unchanged.
/// </summary>
public interface IStructuredLogger : IAppLogger
{
    /// <summary>Sets the current run session context for all subsequent log entries.</summary>
    void SetRunContext(Guid? runSessionId);

    /// <summary>Structured log with optional context properties.</summary>
    void Log(Severity severity, string message, string? source = null, string? url = null,
             double? elapsedMs = null, Exception? ex = null,
             Dictionary<string, string>? properties = null);

    /// <summary>Query persisted log entries from SQLite.</summary>
    Task<List<LogEntry>> QueryLogsAsync(
        DateTime? from = null, DateTime? to = null,
        string? level = null, Guid? runSessionId = null,
        string? sourceFilter = null, int limit = 500,
        CancellationToken ct = default);

    /// <summary>Get log entries for a specific run session.</summary>
    Task<List<LogEntry>> GetRunLogsAsync(Guid runSessionId, CancellationToken ct = default);

    /// <summary>Total count of persisted log entries.</summary>
    Task<long> GetLogCountAsync(CancellationToken ct = default);
}
