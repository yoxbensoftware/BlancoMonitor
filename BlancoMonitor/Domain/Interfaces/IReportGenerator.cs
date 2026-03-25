using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

public interface IReportGenerator
{
    /// <summary>Legacy: generate from in-memory results.</summary>
    Task<string> GenerateAsync(
        List<MonitoringResult> results,
        string outputDirectory,
        CancellationToken ct = default);
}
