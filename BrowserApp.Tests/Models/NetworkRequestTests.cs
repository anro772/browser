using BrowserApp.Core.Models;
using Xunit;

namespace BrowserApp.Tests.Models;

public class NetworkRequestTests
{
    [Theory]
    [InlineData("https://example.com/page", "example.com")]
    [InlineData("https://www.google.com/search?q=test", "www.google.com")]
    [InlineData("http://localhost:3000/api", "localhost")]
    [InlineData("https://sub.domain.example.co.uk/path", "sub.domain.example.co.uk")]
    public void Host_ExtractsCorrectly_FromUrl(string url, string expectedHost)
    {
        var request = new NetworkRequest { Url = url };
        Assert.Equal(expectedHost, request.Host);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("")]
    [InlineData("not a url at all")]
    public void Host_ReturnsEmpty_ForInvalidUrls(string url)
    {
        var request = new NetworkRequest { Url = url };
        Assert.Equal(string.Empty, request.Host);
    }

    [Theory]
    [InlineData(null, "-")]
    [InlineData(0L, "0 B")]
    [InlineData(500L, "500 B")]
    [InlineData(1024L, "1.0 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1.0 MB")]
    [InlineData(1572864L, "1.5 MB")]
    public void FormattedSize_FormatsCorrectly(long? size, string expected)
    {
        var request = new NetworkRequest { Size = size };
        Assert.Equal(expected, request.FormattedSize);
    }

    [Theory]
    [InlineData("https://example.com/", "/")]
    [InlineData("https://example.com/path/to/file.js", "/path/to/file.js")]
    public void ShortUrl_ReturnsPathAndQuery(string url, string expectedPath)
    {
        var request = new NetworkRequest { Url = url };
        Assert.Equal(expectedPath, request.ShortUrl);
    }

    [Fact]
    public void ShortUrl_TruncatesLongPaths()
    {
        // Path longer than 60 chars gets truncated to 57 + "..."
        var longPath = "/very/long/path/that/exceeds/sixty/characters/in/length/for/sure";
        var request = new NetworkRequest { Url = $"https://example.com{longPath}" };

        Assert.EndsWith("...", request.ShortUrl);
        Assert.Equal(60, request.ShortUrl.Length);
    }

    [Fact]
    public void IsThirdParty_ReturnsTrue_WhenHostsDiffer()
    {
        var request = new NetworkRequest { Url = "https://tracker.com/script.js" };
        Assert.True(request.IsThirdParty("example.com"));
    }

    [Fact]
    public void IsThirdParty_ReturnsFalse_WhenHostsMatch()
    {
        var request = new NetworkRequest { Url = "https://example.com/script.js" };
        Assert.False(request.IsThirdParty("example.com"));
    }

    [Fact]
    public void IsThirdParty_ReturnsFalse_WhenSubdomainOfPageHost()
    {
        var request = new NetworkRequest { Url = "https://cdn.example.com/script.js" };
        Assert.False(request.IsThirdParty("example.com"));
    }

    [Fact]
    public void IsThirdParty_ReturnsTrue_WhenPageHostIsSubdomainOfRequestHost()
    {
        // Note: Current implementation only checks if request is subdomain of page,
        // not the reverse. This is expected behavior.
        var request = new NetworkRequest { Url = "https://example.com/script.js" };
        Assert.True(request.IsThirdParty("www.example.com"));
    }

    [Fact]
    public void IsThirdParty_ReturnsFalse_WhenPageHostIsNull()
    {
        var request = new NetworkRequest { Url = "https://example.com/script.js" };
        Assert.False(request.IsThirdParty(null!));
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        var request = new NetworkRequest();

        Assert.Equal(string.Empty, request.Url);
        Assert.Equal("GET", request.Method);
        Assert.Null(request.StatusCode);
        Assert.Equal("Unknown", request.ResourceType);
        Assert.Null(request.ContentType);
        Assert.Null(request.Size);
        Assert.False(request.WasBlocked);
        Assert.Null(request.BlockedByRuleId);
        Assert.True(request.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void InitProperties_CanBeSet()
    {
        var timestamp = DateTime.UtcNow;
        var request = new NetworkRequest
        {
            Url = "https://test.com/api",
            Method = "POST",
            StatusCode = 200,
            ResourceType = "XHR",
            ContentType = "application/json",
            Size = 1024,
            WasBlocked = true,
            BlockedByRuleId = "rule-1",
            Timestamp = timestamp
        };

        Assert.Equal("https://test.com/api", request.Url);
        Assert.Equal("POST", request.Method);
        Assert.Equal(200, request.StatusCode);
        Assert.Equal("XHR", request.ResourceType);
        Assert.Equal("application/json", request.ContentType);
        Assert.Equal(1024, request.Size);
        Assert.True(request.WasBlocked);
        Assert.Equal("rule-1", request.BlockedByRuleId);
        Assert.Equal(timestamp, request.Timestamp);
    }
}
