using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Tests.Analysis;

public sealed class NoiseFilterTests
{
    [Fact]
    public void IsNoise_AnalyticsCategory_ReturnsTrue()
    {
        var trace = new NetworkTrace
        {
            Url = "https://www.google-analytics.com/collect",
            Category = RequestCategory.Analytics,
        };

        Assert.True(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void IsNoise_FontCategory_ReturnsTrue()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/fonts/roboto.woff2",
            Category = RequestCategory.Font,
        };

        Assert.True(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void IsNoise_SmallThirdPartyImage_ReturnsTrue()
    {
        var trace = new NetworkTrace
        {
            Url = "https://tracker.com/pixel.gif",
            Category = RequestCategory.Image,
            IsThirdParty = true,
            ContentLength = 43,
        };

        Assert.True(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void IsNoise_LargeThirdPartyImage_ReturnsFalse()
    {
        var trace = new NetworkTrace
        {
            Url = "https://cdn.example.com/hero.jpg",
            Category = RequestCategory.Image,
            IsThirdParty = true,
            ContentLength = 50000,
        };

        Assert.False(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void IsNoise_KnownNoisePath_ReturnsTrue()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/beacon/track",
            Category = RequestCategory.Other,
        };

        Assert.True(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void IsNoise_PrefetchHeader_ReturnsTrue()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/next-page.html",
            Category = RequestCategory.Html,
            RequestHeaders = new Dictionary<string, string>
            {
                ["Purpose"] = "prefetch"
            },
        };

        Assert.True(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void IsNoise_NormalHtmlRequest_ReturnsFalse()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/about",
            Category = RequestCategory.Html,
        };

        Assert.False(NoiseFilter.IsNoise(trace));
    }

    [Fact]
    public void Partition_SeparatesSignalAndNoise()
    {
        var traces = new List<NetworkTrace>
        {
            new() { Url = "https://example.com/", Category = RequestCategory.Html },
            new() { Url = "https://example.com/style.css", Category = RequestCategory.Css },
            new() { Url = "https://google-analytics.com/collect", Category = RequestCategory.Analytics },
            new() { Url = "https://example.com/font.woff2", Category = RequestCategory.Font },
            new() { Url = "https://example.com/app.js", Category = RequestCategory.JavaScript },
        };

        var (signal, noise) = NoiseFilter.Partition(traces);

        Assert.Equal(3, signal.Count);
        Assert.Equal(2, noise.Count);
        Assert.All(signal, t => Assert.False(NoiseFilter.IsNoise(t)));
        Assert.All(noise, t => Assert.True(NoiseFilter.IsNoise(t)));
    }

    [Fact]
    public void Partition_EmptyInput_ReturnsBothEmpty()
    {
        var (signal, noise) = NoiseFilter.Partition([]);

        Assert.Empty(signal);
        Assert.Empty(noise);
    }
}
