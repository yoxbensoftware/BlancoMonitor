using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Tests.Domain;

public sealed class AlertTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var alert = new Alert();

        Assert.NotEqual(Guid.Empty, alert.Id);
        Assert.Equal(ConfidenceLevel.Suspected, alert.Confidence);
        Assert.Equal(string.Empty, alert.Message);
        Assert.Equal(string.Empty, alert.MetricName);
        Assert.Equal(0, alert.ActualValue);
        Assert.Equal(0, alert.ThresholdValue);
        Assert.Equal(string.Empty, alert.GroupKey);
        Assert.Equal(1, alert.OccurrenceCount);
    }

    [Fact]
    public void CanCreateCriticalAlert()
    {
        var alert = new Alert
        {
            Severity = Severity.Critical,
            Message = "TTFB = 5000ms exceeds critical threshold",
            MetricName = "TimeToFirstByteMs",
            ActualValue = 5000,
            ThresholdValue = 2000,
            Url = "https://example.com",
            Confidence = ConfidenceLevel.Confirmed,
        };

        Assert.Equal(Severity.Critical, alert.Severity);
        Assert.Equal(5000, alert.ActualValue);
        Assert.Equal(2000, alert.ThresholdValue);
        Assert.Equal(ConfidenceLevel.Confirmed, alert.Confidence);
    }
}
