using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

public interface IHistoricalStore
{
    Task SaveAsync(string url, PerformanceMetric metric, CancellationToken ct = default);
    Task<HistoricalRecord?> LoadAsync(string url, CancellationToken ct = default);
    Task<List<HistoricalRecord>> LoadAllAsync(CancellationToken ct = default);
}
