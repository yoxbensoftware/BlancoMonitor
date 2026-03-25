using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.ValueObjects;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Tests.Analysis;

public sealed class RuleEngineTests
{
    private readonly RuleEngineImpl _engine = new();

    private static IReadOnlyDictionary<string, Threshold> DefaultThresholds => new Dictionary<string, Threshold>
    {
        ["TotalTimeMs"] = new("TotalTimeMs", 3000, 10000),
        ["TimeToFirstByteMs"] = new("TimeToFirstByteMs", 1000, 5000),
        ["ContentDownloadMs"] = new("ContentDownloadMs", 2000, 8000),
    };

    [Fact]
    public void Evaluate_NoViolations_ReturnsEmpty()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            TotalTimeMs = 500,
            TimeToFirstByteMs = 200,
            ContentDownloadMs = 300,
            StatusCode = 200,
        };

        var alerts = _engine.Evaluate(metric, DefaultThresholds);
        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_CriticalTTFB_ReturnsCriticalAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            TimeToFirstByteMs = 6000,
            TotalTimeMs = 500,
            ContentDownloadMs = 300,
            StatusCode = 200,
        };

        var alerts = _engine.Evaluate(metric, DefaultThresholds);

        Assert.Contains(alerts, a => a.Severity == Severity.Critical && a.MetricName == "TimeToFirstByteMs");
    }

    [Fact]
    public void Evaluate_WarningTotalTime_ReturnsWarningAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            TotalTimeMs = 5000,
            TimeToFirstByteMs = 200,
            ContentDownloadMs = 300,
            StatusCode = 200,
        };

        var alerts = _engine.Evaluate(metric, DefaultThresholds);

        Assert.Contains(alerts, a => a.Severity == Severity.Warning && a.MetricName == "TotalTimeMs");
    }

    [Fact]
    public void Evaluate_ServerError500_ReturnsCriticalAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            StatusCode = 500,
            TotalTimeMs = 100,
            TimeToFirstByteMs = 50,
            ContentDownloadMs = 50,
        };

        var alerts = _engine.Evaluate(metric, DefaultThresholds);

        Assert.Contains(alerts, a =>
            a.Severity == Severity.Critical &&
            a.MetricName == "StatusCode" &&
            a.ActualValue == 500);
    }

    [Fact]
    public void Evaluate_ClientError404_ReturnsWarningAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com/missing",
            StatusCode = 404,
            TotalTimeMs = 100,
            TimeToFirstByteMs = 50,
            ContentDownloadMs = 50,
        };

        var alerts = _engine.Evaluate(metric, DefaultThresholds);

        Assert.Contains(alerts, a =>
            a.Severity == Severity.Warning &&
            a.MetricName == "StatusCode");
    }

    [Fact]
    public void EvaluateAdvanced_SlowTTFB_GeneratesAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            TimeToFirstByteMs = 8000,
            TotalTimeMs = 9000,
            StatusCode = 200,
            PageType = PageType.Homepage,
        };

        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com",
                ContentType = "text/html",
                StatusCode = 200,
                Category = RequestCategory.Html,
                TimeToFirstByteMs = 8000,
                TotalTimeMs = 9000,
            },
        };

        var alerts = _engine.EvaluateAdvanced(metric, traces, DefaultThresholds);

        Assert.NotEmpty(alerts);
        Assert.Contains(alerts, a => a.MetricName == "TimeToFirstByteMs");
    }

    [Fact]
    public void EvaluateAdvanced_FailedRequests_GeneratesAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            StatusCode = 200,
            TotalRequestCount = 10,
            FailedRequestCount = 3,
            TimeToFirstByteMs = 100,
            TotalTimeMs = 200,
        };

        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com",
                ContentType = "text/html",
                StatusCode = 200,
                Category = RequestCategory.Html,
            },
        };

        var alerts = _engine.EvaluateAdvanced(metric, traces, DefaultThresholds);

        Assert.Contains(alerts, a => a.MetricName == "FailedRequestCount");
    }

    [Fact]
    public void EvaluateAdvanced_Http500_GeneratesCritical()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            StatusCode = 502,
            TimeToFirstByteMs = 100,
            TotalTimeMs = 200,
        };

        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com",
                ContentType = "text/html",
                StatusCode = 502,
                Category = RequestCategory.Html,
            },
        };

        var alerts = _engine.EvaluateAdvanced(metric, traces, DefaultThresholds);

        Assert.Contains(alerts, a =>
            a.Severity == Severity.Critical &&
            a.MetricName == "StatusCode");
    }

    [Fact]
    public void EvaluateAdvanced_LargeTransfer_GeneratesAlert()
    {
        var metric = new PerformanceMetric
        {
            Url = "https://example.com",
            StatusCode = 200,
            TimeToFirstByteMs = 100,
            TotalTimeMs = 200,
            TotalTransferBytes = 6 * 1024 * 1024,
        };

        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com",
                ContentType = "text/html",
                StatusCode = 200,
                Category = RequestCategory.Html,
            },
        };

        var alerts = _engine.EvaluateAdvanced(metric, traces, DefaultThresholds);

        Assert.Contains(alerts, a => a.MetricName == "TotalTransferSize");
    }
}
