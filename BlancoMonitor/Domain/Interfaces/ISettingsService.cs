namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Persists user-level application settings (language, update preferences, window state).
/// Stored separately from the monitoring configuration (blanco_config.json).
/// </summary>
public interface ISettingsService
{
    string Language { get; set; }
    string? LastSkippedVersion { get; set; }
    string? UpdateChannelUrl { get; set; }
    bool CheckForUpdatesOnStartup { get; set; }

    void Load();
    void Save();
}
