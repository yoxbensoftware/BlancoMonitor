using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

public sealed record PerformanceStatistics(
    double AverageMs,
    double MedianMs,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    double MinMs,
    int SampleCount);

public interface IPerformanceAnalyzer
{
    PerformanceMetric Analyze(IReadOnlyList<NetworkTrace> traces, string url);
    PerformanceStatistics ComputeStatistics(IReadOnlyList<double> values);
}
