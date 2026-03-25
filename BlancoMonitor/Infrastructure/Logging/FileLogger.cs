using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Logging;

public sealed class FileLogger : IAppLogger, IDisposable
{
    private readonly string _logDirectory;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string _currentLogFile = string.Empty;

    public event Action<DateTime, Severity, string>? LogEntryAdded;

    public FileLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
        RotateLogFile();
    }

    public void Log(Severity severity, string message)
    {
        var timestamp = DateTime.Now;
        var entry = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{severity,-8}] {message}";

        lock (_lock)
        {
            EnsureLogFile();
            _writer?.WriteLine(entry);
            _writer?.Flush();
        }

        LogEntryAdded?.Invoke(timestamp, severity, message);
    }

    public void Info(string message) => Log(Severity.Info, message);
    public void Warning(string message) => Log(Severity.Warning, message);

    public void Error(string message, Exception? ex = null)
    {
        var full = ex is not null ? $"{message} | {ex.GetType().Name}: {ex.Message}" : message;
        Log(Severity.Critical, full);
    }

    private void EnsureLogFile()
    {
        var expectedFile = Path.Combine(_logDirectory, $"blanco_{DateTime.Now:yyyyMMdd}.log");
        if (expectedFile != _currentLogFile)
            RotateLogFile();
    }

    private void RotateLogFile()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _currentLogFile = Path.Combine(_logDirectory, $"blanco_{DateTime.Now:yyyyMMdd}.log");
            _writer = new StreamWriter(_currentLogFile, append: true) { AutoFlush = true };
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
