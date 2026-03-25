using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Tests.Domain;

public sealed class MonitorTargetTests
{
    [Fact]
    public void NewTarget_HasUniqueId()
    {
        var t1 = new MonitorTarget();
        var t2 = new MonitorTarget();

        Assert.NotEqual(Guid.Empty, t1.Id);
        Assert.NotEqual(t1.Id, t2.Id);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var target = new MonitorTarget();

        Assert.Equal(string.Empty, target.Name);
        Assert.Equal(string.Empty, target.Url);
        Assert.Empty(target.Keywords);
        Assert.True(target.IsEnabled);
        Assert.Equal(60, target.CheckIntervalSeconds);
    }

    [Fact]
    public void CanSetProperties()
    {
        var target = new MonitorTarget
        {
            Name = "Test Site",
            Url = "https://example.com",
            Keywords = ["perf", "speed"],
            IsEnabled = false,
            CheckIntervalSeconds = 120,
        };

        Assert.Equal("Test Site", target.Name);
        Assert.Equal("https://example.com", target.Url);
        Assert.Equal(2, target.Keywords.Count);
        Assert.False(target.IsEnabled);
        Assert.Equal(120, target.CheckIntervalSeconds);
    }
}
