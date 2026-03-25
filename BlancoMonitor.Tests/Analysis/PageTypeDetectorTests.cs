using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Infrastructure.Analysis;

namespace BlancoMonitor.Tests.Analysis;

public sealed class PageTypeDetectorTests
{
    [Theory]
    [InlineData("https://example.com/", PageType.Homepage)]
    [InlineData("https://example.com", PageType.Homepage)]
    [InlineData("https://example.com/index.html", PageType.Homepage)]
    [InlineData("https://example.com/home", PageType.Homepage)]
    public void Detect_Homepage(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/search?q=test", PageType.Search)]
    [InlineData("https://example.com/suche?query=keyword", PageType.Search)]
    [InlineData("https://example.com/arama?q=blanco", PageType.Search)]
    public void Detect_SearchPage(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/product/123", PageType.Product)]
    [InlineData("https://example.com/item/abc", PageType.Product)]
    [InlineData("https://example.com/urun/detail", PageType.Product)]
    public void Detect_ProductPage(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/category/electronics", PageType.Category)]
    [InlineData("https://example.com/shop/clothing", PageType.Category)]
    [InlineData("https://example.com/kategori/mutfak", PageType.Category)]
    public void Detect_CategoryPage(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/checkout", PageType.Checkout)]
    [InlineData("https://example.com/cart", PageType.Checkout)]
    [InlineData("https://example.com/sepet", PageType.Checkout)]
    public void Detect_CheckoutPage(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/api/v1/users", PageType.Api)]
    [InlineData("https://example.com/graphql", PageType.Api)]
    public void Detect_ApiEndpoint(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/static/bundle.js", PageType.Static)]
    [InlineData("https://example.com/assets/logo.png", PageType.Static)]
    public void Detect_StaticResource(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Theory]
    [InlineData("https://example.com/about", PageType.Unknown)]
    [InlineData("https://example.com/contact-us", PageType.Unknown)]
    public void Detect_UnknownPage(string url, PageType expected)
    {
        Assert.Equal(expected, PageTypeDetector.Detect(url));
    }

    [Fact]
    public void Detect_InvalidUrl_ReturnsUnknown()
    {
        Assert.Equal(PageType.Unknown, PageTypeDetector.Detect("not-a-url"));
    }
}
