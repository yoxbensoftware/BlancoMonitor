using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Application.Services;

public sealed class UrlKeywordSetManager
{
    private readonly IConfigurationStore _store;
    private readonly IAppLogger _logger;
    private AppConfiguration _config = new();

    public IReadOnlyList<MonitorTarget> Targets => _config.Targets;

    public UrlKeywordSetManager(IConfigurationStore store, IAppLogger logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        _config = await _store.LoadAsync(ct);
        _logger.Info($"Loaded {_config.Targets.Count} monitor targets");
    }

    public async Task AddTargetAsync(MonitorTarget target, CancellationToken ct = default)
    {
        _config.Targets.Add(target);
        await _store.SaveAsync(_config, ct);
        _logger.Info($"Added target: {target.Name} ({target.Url})");
    }

    public async Task RemoveTargetAsync(Guid targetId, CancellationToken ct = default)
    {
        var removed = _config.Targets.RemoveAll(t => t.Id == targetId);
        if (removed > 0)
        {
            await _store.SaveAsync(_config, ct);
            _logger.Info($"Removed target: {targetId}");
        }
    }

    public async Task UpdateTargetAsync(MonitorTarget target, CancellationToken ct = default)
    {
        var index = _config.Targets.FindIndex(t => t.Id == target.Id);
        if (index >= 0)
        {
            _config.Targets[index] = target;
            await _store.SaveAsync(_config, ct);
            _logger.Info($"Updated target: {target.Name}");
        }
    }

    public AppConfiguration GetConfiguration() => _config;

    public async Task SaveConfigurationAsync(AppConfiguration config, CancellationToken ct = default)
    {
        _config = config;
        await _store.SaveAsync(_config, ct);
    }
}
