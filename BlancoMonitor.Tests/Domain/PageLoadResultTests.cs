using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Tests.Domain;

public sealed class PageLoadResultTests
{
    [Fact]
    public void AllTraces_IncludesPrimaryAndSubResources()
    {
        var result = new PageLoadResult
        {
            PrimaryTrace = new NetworkTrace { Url = "https://example.com", StatusCode = 200 },
            SubResourceTraces =
            [
                new NetworkTrace { Url = "https://example.com/style.css", StatusCode = 200 },
                new NetworkTrace { Url = "https://example.com/app.js", StatusCode = 200 },
            ],
        };

        Assert.Equal(3, result.AllTraces.Count);
        Assert.Equal("https://example.com", result.AllTraces[0].Url);
        Assert.Equal("https://example.com/style.css", result.AllTraces[1].Url);
    }

    [Fact]
    public void TotalRequestCount_ReturnsCorrectValue()
    {
        var result = new PageLoadResult
        {
            PrimaryTrace = new NetworkTrace(),
            SubResourceTraces =
            [
                new NetworkTrace(),
                new NetworkTrace(),
                new NetworkTrace(),
            ],
        };

        Assert.Equal(4, result.TotalRequestCount);
    }

    [Fact]
    public void FailedRequestCount_CountsErrorsAndStatusCodes()
    {
        var result = new PageLoadResult
        {
            PrimaryTrace = new NetworkTrace { StatusCode = 200 },
            SubResourceTraces =
            [
                new NetworkTrace { StatusCode = 404 },
                new NetworkTrace { StatusCode = 200, ErrorMessage = "timeout" },
                new NetworkTrace { StatusCode = 200 },
            ],
        };

        Assert.Equal(2, result.FailedRequestCount);
    }

    [Fact]
    public void FailedRequestCount_ZeroWhenAllSuccessful()
    {
        var result = new PageLoadResult
        {
            PrimaryTrace = new NetworkTrace { StatusCode = 200 },
            SubResourceTraces =
            [
                new NetworkTrace { StatusCode = 200 },
                new NetworkTrace { StatusCode = 301 },
            ],
        };

        Assert.Equal(0, result.FailedRequestCount);
    }

    [Fact]
    public void AllTraces_EmptySubResources_ContainsOnlyPrimary()
    {
        var result = new PageLoadResult
        {
            PrimaryTrace = new NetworkTrace { Url = "https://example.com" },
        };

        Assert.Single(result.AllTraces);
        Assert.Equal(1, result.TotalRequestCount);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var result = new PageLoadResult();

        Assert.NotNull(result.PrimaryTrace);
        Assert.Empty(result.SubResourceTraces);
        Assert.Equal(PageType.Unknown, result.DetectedPageType);
        Assert.Equal(0, result.FullPageLoadMs);
        Assert.Null(result.HtmlContent);
    }
}
