using FluentAssertions;
using BrowserApp.Core.Services;

namespace BrowserApp.Tests.Services;

/// <summary>
/// Unit tests for SearchEngineService.
/// Tests URL vs search query detection and URL formatting.
/// </summary>
public class SearchEngineServiceTests
{
    private readonly SearchEngineService _service;

    public SearchEngineServiceTests()
    {
        _service = new SearchEngineService();
    }

    #region IsValidUrl Tests

    [Theory]
    [InlineData("https://google.com", true)]
    [InlineData("http://example.com", true)]
    [InlineData("google.com", true)]
    [InlineData("www.google.com", true)]
    [InlineData("subdomain.example.com", true)]
    [InlineData("example.co.uk", true)]
    [InlineData("localhost", true)]
    [InlineData("localhost:3000", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.1.1:8080", true)]
    public void IsValidUrl_ValidUrls_ReturnsTrue(string input, bool expected)
    {
        // Act
        bool result = _service.IsValidUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("how to bake bread", false)]
    [InlineData("what is the weather", false)]
    [InlineData("hello world", false)]
    [InlineData("search query with spaces", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidUrl_SearchQueries_ReturnsFalse(string input, bool expected)
    {
        // Act
        bool result = _service.IsValidUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetNavigationUrl Tests

    [Theory]
    [InlineData("https://google.com", "https://google.com")]
    [InlineData("http://example.com", "http://example.com")]
    public void GetNavigationUrl_FullUrls_ReturnsUnchanged(string input, string expected)
    {
        // Act
        string result = _service.GetNavigationUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("google.com", "https://google.com")]
    [InlineData("www.example.com", "https://www.example.com")]
    [InlineData("subdomain.example.co.uk", "https://subdomain.example.co.uk")]
    public void GetNavigationUrl_DomainWithoutProtocol_AddsHttps(string input, string expected)
    {
        // Act
        string result = _service.GetNavigationUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetNavigationUrl_SearchQuery_ReturnsGoogleSearchUrl()
    {
        // Arrange
        string query = "how to bake bread";

        // Act
        string result = _service.GetNavigationUrl(query);

        // Assert
        result.Should().StartWith("https://www.google.com/search?q=");
        result.Should().Contain("how");
        result.Should().Contain("bake");
        result.Should().Contain("bread");
    }

    [Fact]
    public void GetNavigationUrl_SearchQueryWithSpecialChars_EncodesUrl()
    {
        // Arrange
        string query = "c# programming & .NET";

        // Act
        string result = _service.GetNavigationUrl(query);

        // Assert
        result.Should().StartWith("https://www.google.com/search?q=");
        result.Should().Contain("%23"); // URL encoded #
        result.Should().Contain("%26"); // URL encoded &
    }

    [Theory]
    [InlineData("localhost", "http://localhost")]
    [InlineData("localhost:3000", "http://localhost:3000")]
    [InlineData("localhost:8080/api", "http://localhost:8080/api")]
    public void GetNavigationUrl_Localhost_ReturnsHttp(string input, string expected)
    {
        // Act
        string result = _service.GetNavigationUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("192.168.1.1", "http://192.168.1.1")]
    [InlineData("192.168.1.1:8080", "http://192.168.1.1:8080")]
    [InlineData("10.0.0.1/admin", "http://10.0.0.1/admin")]
    public void GetNavigationUrl_IpAddress_ReturnsHttp(string input, string expected)
    {
        // Act
        string result = _service.GetNavigationUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
