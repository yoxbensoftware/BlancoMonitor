using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Tests.Domain;

public sealed class NetworkTraceTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var trace = new NetworkTrace();

        Assert.NotEqual(Guid.Empty, trace.Id);
        Assert.Equal(string.Empty, trace.Url);
        Assert.Equal("GET", trace.Method);
        Assert.Equal(0, trace.StatusCode);
        Assert.Empty(trace.RequestHeaders);
        Assert.Empty(trace.ResponseHeaders);
        Assert.Equal(0, trace.TimeToFirstByteMs);
        Assert.Equal(0, trace.TotalTimeMs);
        Assert.Null(trace.ContentType);
        Assert.Empty(trace.RedirectChain);
        Assert.Null(trace.ErrorMessage);
        Assert.Equal(RequestCategory.Other, trace.Category);
        Assert.False(trace.IsThirdParty);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/api/data",
            Method = "POST",
            StatusCode = 201,
            TimeToFirstByteMs = 50,
            TotalTimeMs = 120,
            ContentDownloadMs = 70,
            ContentType = "application/json",
            ContentLength = 1024,
            Category = RequestCategory.Api,
            IsThirdParty = true,
            ErrorMessage = null,
            InitiatorUrl = "https://example.com",
            RedirectChain = ["https://example.com/old-api"],
        };

        Assert.Equal("POST", trace.Method);
        Assert.Equal(201, trace.StatusCode);
        Assert.Equal(50, trace.TimeToFirstByteMs);
        Assert.Equal(120, trace.TotalTimeMs);
        Assert.Equal(70, trace.ContentDownloadMs);
        Assert.Equal(RequestCategory.Api, trace.Category);
        Assert.True(trace.IsThirdParty);
        Assert.Single(trace.RedirectChain);
    }
}
