using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Interfaces;

public interface IAppLogger
{
    void Log(Severity severity, string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);

    event Action<DateTime, Severity, string>? LogEntryAdded;
}
