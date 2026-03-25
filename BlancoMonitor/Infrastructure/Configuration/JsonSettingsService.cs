using System.Text.Json;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Configuration;

/// <summary>
/// Persists user-level settings to a JSON file (blanco_settings.json).
/// Separate from monitoring configuration to avoid merge conflicts.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;
    private SettingsData _data = new();

    public JsonSettingsService(string filePath = "blanco_settings.json")
    {
        _filePath = filePath;
        Load();
    }

    // ── ISettingsService properties ─────────────────────────────

    public string Language
    {
        get => _data.Language;
        set => _data.Language = value;
    }

    public string? LastSkippedVersion
    {
        get => _data.LastSkippedVersion;
        set => _data.LastSkippedVersion = value;
    }

    public string? UpdateChannelUrl
    {
        get => _data.UpdateChannelUrl;
        set => _data.UpdateChannelUrl = value;
    }

    public bool CheckForUpdatesOnStartup
    {
        get => _data.CheckForUpdatesOnStartup;
        set => _data.CheckForUpdatesOnStartup = value;
    }

    // ── Persistence ─────────────────────────────────────────────

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            _data = new SettingsData();
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _data = JsonSerializer.Deserialize<SettingsData>(json, JsonOpts) ?? new SettingsData();
        }
        catch
        {
            _data = new SettingsData();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, JsonOpts);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Swallow — settings save should never crash the app
        }
    }

    // ── Internal data model ─────────────────────────────────────

    private sealed class SettingsData
    {
        public string Language { get; set; } = "en";
        public string? LastSkippedVersion { get; set; }
        public string? UpdateChannelUrl { get; set; }
        public bool CheckForUpdatesOnStartup { get; set; } = true;
    }
}
