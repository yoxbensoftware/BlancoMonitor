using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Tests.Analysis;

public sealed class RequestClassifierTests
{
    [Fact]
    public void Classify_HtmlContentType_ReturnsHtml()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/page",
            ContentType = "text/html; charset=utf-8",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Html, result);
    }

    [Fact]
    public void Classify_JsonContentType_ReturnsApi()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/api/data",
            ContentType = "application/json",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Api, result);
    }

    [Fact]
    public void Classify_CssContentType_ReturnsCss()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/style.css",
            ContentType = "text/css",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Css, result);
    }

    [Fact]
    public void Classify_JavaScriptContentType_ReturnsJavaScript()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/app.js",
            ContentType = "application/javascript",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.JavaScript, result);
    }

    [Fact]
    public void Classify_ImageContentType_ReturnsImage()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/logo.png",
            ContentType = "image/png",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Image, result);
    }

    [Fact]
    public void Classify_FontContentType_ReturnsFont()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/font.woff2",
            ContentType = "font/woff2",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Font, result);
    }

    [Fact]
    public void Classify_AnalyticsDomain_ReturnsAnalytics()
    {
        var trace = new NetworkTrace
        {
            Url = "https://www.google-analytics.com/collect",
            ContentType = "image/gif",
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Analytics, result);
    }

    [Fact]
    public void Classify_ThirdParty_SetsIsThirdPartyFlag()
    {
        var trace = new NetworkTrace
        {
            Url = "https://cdn.other-site.com/lib.js",
            ContentType = "application/javascript",
        };

        RequestClassifier.Classify(trace, "example.com");
        Assert.True(trace.IsThirdParty);
    }

    [Fact]
    public void Classify_SameHost_DoesNotSetThirdParty()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/app.js",
            ContentType = "application/javascript",
        };

        RequestClassifier.Classify(trace, "example.com");
        Assert.False(trace.IsThirdParty);
    }

    [Fact]
    public void ClassifyAll_ClassifiesMultipleTraces()
    {
        var traces = new NetworkTrace[]
        {
            new() { Url = "https://example.com/", ContentType = "text/html" },
            new() { Url = "https://example.com/style.css", ContentType = "text/css" },
            new() { Url = "https://example.com/app.js", ContentType = "application/javascript" },
        };

        RequestClassifier.ClassifyAll(traces, "example.com");

        Assert.Equal(RequestCategory.Html, traces[0].Category);
        Assert.Equal(RequestCategory.Css, traces[1].Category);
        Assert.Equal(RequestCategory.JavaScript, traces[2].Category);
    }

    [Fact]
    public void Classify_NoContentType_FallsBackToUrlPattern()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/image.jpg",
            ContentType = null,
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Image, result);
    }

    [Fact]
    public void Classify_WoffExtension_ReturnsFont()
    {
        var trace = new NetworkTrace
        {
            Url = "https://example.com/fonts/roboto.woff2",
            ContentType = null,
        };

        var result = RequestClassifier.Classify(trace);
        Assert.Equal(RequestCategory.Font, result);
    }
}
