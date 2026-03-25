using System.Text.Json;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Configuration;

public sealed class JsonConfigurationManager : IConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;

    public JsonConfigurationManager(string filePath = "blanco_config.json")
    {
        _filePath = filePath;
    }

    public async Task<AppConfiguration> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return new AppConfiguration();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);
            return JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions) ?? new AppConfiguration();
        }
        catch
        {
            return new AppConfiguration();
        }
    }

    public async Task SaveAsync(AppConfiguration config, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }
}
