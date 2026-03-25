using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.ValueObjects;

public sealed record Threshold(
    string MetricName,
    double WarningValue,
    double CriticalValue,
    ComparisonOperator Operator = ComparisonOperator.GreaterThan);
