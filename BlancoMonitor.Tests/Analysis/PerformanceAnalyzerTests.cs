using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Tests.Analysis;

public sealed class PerformanceAnalyzerTests
{
    private readonly PerformanceAnalyzerImpl _analyzer = new();

    [Fact]
    public void Analyze_EmptyTraces_ReturnsDefaultMetric()
    {
        var metric = _analyzer.Analyze([], "https://example.com");

        Assert.Equal("https://example.com", metric.Url);
        Assert.Equal(0, metric.TimeToFirstByteMs);
        Assert.Equal(0, metric.TotalTimeMs);
        Assert.Equal(0, metric.TotalRequestCount);
    }

    [Fact]
    public void Analyze_SingleHtmlTrace_ReturnsCorrectTimings()
    {
        var now = DateTime.UtcNow;
        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com/",
                ContentType = "text/html",
                StatusCode = 200,
                TimeToFirstByteMs = 150,
                TotalTimeMs = 300,
                ContentDownloadMs = 150,
                ContentLength = 5000,
                Timestamp = now,
            },
        };

        var metric = _analyzer.Analyze(traces, "https://example.com");

        Assert.Equal(150, metric.TimeToFirstByteMs);
        Assert.Equal(300, metric.TotalTimeMs);
        Assert.Equal(150, metric.ContentDownloadMs);
        Assert.Equal(200, metric.StatusCode);
        Assert.Equal(5000, metric.ContentLength);
        Assert.Equal(1, metric.TotalRequestCount);
        Assert.Equal(0, metric.FailedRequestCount);
    }

    [Fact]
    public void Analyze_MultipleTraces_CalculatesBreakdowns()
    {
        var now = DateTime.UtcNow;
        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com/",
                ContentType = "text/html",
                StatusCode = 200,
                TimeToFirstByteMs = 100,
                TotalTimeMs = 200,
                ContentLength = 10000,
                Timestamp = now,
            },
            new()
            {
                Url = "https://example.com/style.css",
                ContentType = "text/css",
                StatusCode = 200,
                TotalTimeMs = 50,
                ContentLength = 2000,
                Timestamp = now,
            },
            new()
            {
                Url = "https://example.com/app.js",
                ContentType = "application/javascript",
                StatusCode = 200,
                TotalTimeMs = 80,
                ContentLength = 30000,
                Timestamp = now,
            },
        };

        var metric = _analyzer.Analyze(traces, "https://example.com");

        Assert.Equal(3, metric.TotalRequestCount);
        Assert.Equal(0, metric.FailedRequestCount);
        Assert.True(metric.ResourceBreakdowns.ContainsKey(RequestCategory.Html));
        Assert.True(metric.ResourceBreakdowns.ContainsKey(RequestCategory.Css));
        Assert.True(metric.ResourceBreakdowns.ContainsKey(RequestCategory.JavaScript));
        Assert.Equal(42000, metric.TotalTransferBytes);
    }

    [Fact]
    public void Analyze_WithFailedRequests_CountsThem()
    {
        var now = DateTime.UtcNow;
        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com/",
                ContentType = "text/html",
                StatusCode = 200,
                Timestamp = now,
            },
            new()
            {
                Url = "https://example.com/missing.js",
                ContentType = "application/javascript",
                StatusCode = 404,
                Timestamp = now,
            },
            new()
            {
                Url = "https://example.com/broken.css",
                ErrorMessage = "Connection refused",
                Timestamp = now,
            },
        };

        var metric = _analyzer.Analyze(traces, "https://example.com");

        Assert.Equal(3, metric.TotalRequestCount);
        Assert.Equal(2, metric.FailedRequestCount);
    }

    [Fact]
    public void Analyze_WithThirdParty_CountsThem()
    {
        var now = DateTime.UtcNow;
        var traces = new List<NetworkTrace>
        {
            new()
            {
                Url = "https://example.com/",
                ContentType = "text/html",
                StatusCode = 200,
                Timestamp = now,
            },
            new()
            {
                Url = "https://cdn.other.com/lib.js",
                ContentType = "application/javascript",
                StatusCode = 200,
                Timestamp = now,
            },
        };

        var metric = _analyzer.Analyze(traces, "https://example.com");

        Assert.True(metric.ThirdPartyRequestCount > 0);
    }

    [Fact]
    public void ComputeStatistics_EmptyValues_ReturnsZeros()
    {
        var stats = _analyzer.ComputeStatistics([]);

        Assert.Equal(0, stats.AverageMs);
        Assert.Equal(0, stats.MedianMs);
        Assert.Equal(0, stats.P95Ms);
        Assert.Equal(0, stats.MaxMs);
        Assert.Equal(0, stats.MinMs);
        Assert.Equal(0, stats.SampleCount);
    }

    [Fact]
    public void ComputeStatistics_SingleValue_AllFieldsEqual()
    {
        var stats = _analyzer.ComputeStatistics([42.0]);

        Assert.Equal(42.0, stats.AverageMs);
        Assert.Equal(42.0, stats.MedianMs);
        Assert.Equal(42.0, stats.P95Ms);
        Assert.Equal(42.0, stats.MaxMs);
        Assert.Equal(42.0, stats.MinMs);
        Assert.Equal(1, stats.SampleCount);
    }

    [Fact]
    public void ComputeStatistics_MultipleValues_CorrectCalculations()
    {
        var values = new List<double> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        var stats = _analyzer.ComputeStatistics(values);

        Assert.Equal(55, stats.AverageMs);
        Assert.Equal(10, stats.MinMs);
        Assert.Equal(100, stats.MaxMs);
        Assert.Equal(10, stats.SampleCount);
        Assert.True(stats.MedianMs > 0);
        Assert.True(stats.P95Ms >= stats.MedianMs);
    }
}
