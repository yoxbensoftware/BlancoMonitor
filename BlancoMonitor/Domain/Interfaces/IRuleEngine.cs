using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.ValueObjects;

namespace BlancoMonitor.Domain.Interfaces;

public interface IRuleEngine
{
    List<Alert> Evaluate(PerformanceMetric metric, IReadOnlyDictionary<string, Threshold> thresholds);

    /// <summary>
    /// Advanced evaluation with per-page-type thresholds, confidence levels,
    /// noise filtering, and issue grouping.
    /// </summary>
    List<Alert> EvaluateAdvanced(
        PerformanceMetric metric,
        IReadOnlyList<NetworkTrace> traces,
        IReadOnlyDictionary<string, Threshold> thresholds,
        IReadOnlyList<Alert>? priorAlerts = null);
}
