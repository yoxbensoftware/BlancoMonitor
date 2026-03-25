using System.Text.Json;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using Microsoft.Data.Sqlite;

namespace BlancoMonitor.Infrastructure.Logging;

/// <summary>
/// Structured logger that writes to daily log files AND SQLite for queryable analysis.
/// Implements both IAppLogger (backward-compatible) and IStructuredLogger (advanced).
/// </summary>
public sealed class StructuredLogger : IStructuredLogger, IDisposable
{
    private readonly string _logDirectory;
    private readonly string _dbPath;
    private readonly object _fileLock = new();
    private readonly object _dbLock = new();
    private StreamWriter? _writer;
    private string _currentLogFile = string.Empty;
    private SqliteConnection? _connection;
    private Guid? _currentRunSessionId;

    public event Action<DateTime, Severity, string>? LogEntryAdded;

    public StructuredLogger(string logDirectory, string dbPath)
    {
        _logDirectory = logDirectory;
        _dbPath = dbPath;
        Directory.CreateDirectory(_logDirectory);
        RotateLogFile();
        InitializeDatabase();
    }

    // ── IAppLogger ──────────────────────────────────────────────

    public void Log(Severity severity, string message)
    {
        Log(severity, message, source: null, url: null, elapsedMs: null, ex: null, properties: null);
    }

    public void Info(string message) => Log(Severity.Info, message);
    public void Warning(string message) => Log(Severity.Warning, message);

    public void Error(string message, Exception? ex = null)
    {
        Log(Severity.Critical, message, source: null, url: null, elapsedMs: null, ex: ex, properties: null);
    }

    // ── IStructuredLogger ───────────────────────────────────────

    public void SetRunContext(Guid? runSessionId) => _currentRunSessionId = runSessionId;

    public void Log(Severity severity, string message, string? source = null,
                    string? url = null, double? elapsedMs = null, Exception? ex = null,
                    Dictionary<string, string>? properties = null)
    {
        var timestamp = DateTime.UtcNow;
        var exceptionText = ex is not null ? $"{ex.GetType().Name}: {ex.Message}" : null;

        // ── File output ─────────────────────────────────────────
        var fileLine = FormatLogLine(timestamp, severity, message, source, url, elapsedMs, exceptionText);
        lock (_fileLock)
        {
            EnsureLogFile();
            _writer?.WriteLine(fileLine);
            _writer?.Flush();
        }

        // ── SQLite output ───────────────────────────────────────
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            Level = severity.ToString(),
            Message = message,
            Source = source,
            Exception = exceptionText,
            RunSessionId = _currentRunSessionId,
            Url = url,
            ElapsedMs = elapsedMs,
            Properties = properties ?? [],
        };

        try { PersistLogEntry(entry); }
        catch { /* never fail the caller because of logging */ }

        // ── Event ───────────────────────────────────────────────
        LogEntryAdded?.Invoke(timestamp, severity, message);
    }

    public async Task<List<LogEntry>> QueryLogsAsync(
        DateTime? from = null, DateTime? to = null,
        string? level = null, Guid? runSessionId = null,
        string? sourceFilter = null, int limit = 500,
        CancellationToken ct = default)
    {
        var results = new List<LogEntry>();
        var sql = "SELECT Id, Timestamp, Level, Message, Source, Exception, RunSessionId, Url, ElapsedMs, PropertiesJson FROM LogEntries WHERE 1=1";
        var parameters = new List<SqliteParameter>();

        if (from.HasValue) { sql += " AND Timestamp >= @from"; parameters.Add(new("@from", from.Value.ToString("o"))); }
        if (to.HasValue) { sql += " AND Timestamp <= @to"; parameters.Add(new("@to", to.Value.ToString("o"))); }
        if (level is not null) { sql += " AND Level = @level"; parameters.Add(new("@level", level)); }
        if (runSessionId.HasValue) { sql += " AND RunSessionId = @runId"; parameters.Add(new("@runId", runSessionId.Value.ToString())); }
        if (sourceFilter is not null) { sql += " AND Source LIKE @source"; parameters.Add(new("@source", $"%{sourceFilter}%")); }

        sql += " ORDER BY Timestamp DESC LIMIT @limit";
        parameters.Add(new("@limit", limit));

        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(ReadLogEntry(reader));
        }

        return results;
    }

    public async Task<List<LogEntry>> GetRunLogsAsync(Guid runSessionId, CancellationToken ct = default)
    {
        return await QueryLogsAsync(runSessionId: runSessionId, limit: 5000, ct: ct);
    }

    public async Task<long> GetLogCountAsync(CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM LogEntries";
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is long count ? count : 0;
    }

    // ── Internals ───────────────────────────────────────────────

    private void InitializeDatabase()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (dir is not null) Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();

        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
        pragma.ExecuteNonQuery();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS LogEntries (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp       TEXT    NOT NULL,
                Level           TEXT    NOT NULL,
                Message         TEXT    NOT NULL,
                Source          TEXT,
                Exception       TEXT,
                RunSessionId    TEXT,
                Url             TEXT,
                ElapsedMs       REAL,
                PropertiesJson  TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_LogEntries_Timestamp ON LogEntries(Timestamp);
            CREATE INDEX IF NOT EXISTS IX_LogEntries_Level ON LogEntries(Level);
            CREATE INDEX IF NOT EXISTS IX_LogEntries_RunSessionId ON LogEntries(RunSessionId);
            """;
        cmd.ExecuteNonQuery();
    }

    private void PersistLogEntry(LogEntry entry)
    {
        lock (_dbLock)
        {
            if (_connection is null) return;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO LogEntries (Timestamp, Level, Message, Source, Exception, RunSessionId, Url, ElapsedMs, PropertiesJson)
                VALUES (@ts, @lv, @msg, @src, @ex, @run, @url, @ms, @props)
                """;
            cmd.Parameters.AddWithValue("@ts", entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@lv", entry.Level);
            cmd.Parameters.AddWithValue("@msg", entry.Message);
            cmd.Parameters.AddWithValue("@src", (object?)entry.Source ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ex", (object?)entry.Exception ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@run", entry.RunSessionId.HasValue ? entry.RunSessionId.Value.ToString() : DBNull.Value);
            cmd.Parameters.AddWithValue("@url", (object?)entry.Url ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ms", entry.ElapsedMs.HasValue ? entry.ElapsedMs.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@props", entry.Properties.Count > 0 ? JsonSerializer.Serialize(entry.Properties) : DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private static LogEntry ReadLogEntry(SqliteDataReader reader)
    {
        var entry = new LogEntry
        {
            Id = reader.GetInt64(0),
            Timestamp = DateTime.Parse(reader.GetString(1)),
            Level = reader.GetString(2),
            Message = reader.GetString(3),
            Source = reader.IsDBNull(4) ? null : reader.GetString(4),
            Exception = reader.IsDBNull(5) ? null : reader.GetString(5),
            RunSessionId = reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6)),
            Url = reader.IsDBNull(7) ? null : reader.GetString(7),
            ElapsedMs = reader.IsDBNull(8) ? null : reader.GetDouble(8),
        };

        if (!reader.IsDBNull(9))
        {
            try
            {
                entry.Properties = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(9)) ?? [];
            }
            catch { /* ignore deserialization errors */ }
        }

        return entry;
    }

    private static string FormatLogLine(DateTime timestamp, Severity severity, string message,
                                        string? source, string? url, double? elapsedMs, string? exception)
    {
        var parts = new List<string>
        {
            $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}]",
            $"[{severity,-8}]"
        };

        if (source is not null) parts.Add($"[{source}]");
        if (url is not null) parts.Add($"[{url}]");
        if (elapsedMs.HasValue) parts.Add($"[{elapsedMs.Value:F1}ms]");

        parts.Add(message);

        if (exception is not null) parts.Add($"| {exception}");

        return string.Join(" ", parts);
    }

    private void EnsureLogFile()
    {
        var expectedFile = Path.Combine(_logDirectory, $"blanco_{DateTime.Now:yyyyMMdd}.log");
        if (expectedFile != _currentLogFile)
            RotateLogFile();
    }

    private void RotateLogFile()
    {
        lock (_fileLock)
        {
            _writer?.Dispose();
            _currentLogFile = Path.Combine(_logDirectory, $"blanco_{DateTime.Now:yyyyMMdd}.log");
            _writer = new StreamWriter(_currentLogFile, append: true) { AutoFlush = true };
        }
    }

    public void Dispose()
    {
        lock (_fileLock) { _writer?.Dispose(); _writer = null; }
        lock (_dbLock) { _connection?.Dispose(); _connection = null; }
    }
}
