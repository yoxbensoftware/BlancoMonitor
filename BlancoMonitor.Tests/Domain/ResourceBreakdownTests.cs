using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Tests.Domain;

public sealed class ResourceBreakdownTests
{
    [Fact]
    public void AvgDurationMs_WithMultipleRequests_CalculatesCorrectly()
    {
        var breakdown = new ResourceBreakdown
        {
            Count = 4,
            TotalDurationMs = 400,
            TotalBytes = 10000,
            FailedCount = 1,
        };

        Assert.Equal(100, breakdown.AvgDurationMs);
    }

    [Fact]
    public void AvgDurationMs_ZeroCount_ReturnsZero()
    {
        var breakdown = new ResourceBreakdown
        {
            Count = 0,
            TotalDurationMs = 0,
        };

        Assert.Equal(0, breakdown.AvgDurationMs);
    }
}
