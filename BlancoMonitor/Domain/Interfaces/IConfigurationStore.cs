using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

public interface IConfigurationStore
{
    Task<AppConfiguration> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppConfiguration config, CancellationToken ct = default);
}
