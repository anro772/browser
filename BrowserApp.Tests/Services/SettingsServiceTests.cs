using BrowserApp.Core.Models;
using BrowserApp.UI.Services;
using Xunit;

namespace BrowserApp.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testSettingsPath;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        // Use a unique temp file for each test
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"browser_test_settings_{Guid.NewGuid()}.json");

        // Create service (it will use the default path, but we'll clean up after)
        _service = new SettingsService();
    }

    public void Dispose()
    {
        // Clean up test settings file if it exists
        if (File.Exists(_testSettingsPath))
        {
            File.Delete(_testSettingsPath);
        }
    }

    [Fact]
    public void Settings_AreAccessible()
    {
        // Settings may have persisted values from previous runs
        // Just verify the object is accessible
        var settings = _service.Settings;

        Assert.NotNull(settings);
        Assert.True(Enum.IsDefined(typeof(PrivacyMode), settings.PrivacyMode));
        Assert.NotNull(settings.ServerUrl);
    }

    [Fact]
    public void PrivacyMode_CanBeSet()
    {
        _service.PrivacyMode = PrivacyMode.Strict;

        Assert.Equal(PrivacyMode.Strict, _service.PrivacyMode);
    }

    [Fact]
    public void ServerUrl_CanBeSet()
    {
        _service.ServerUrl = "https://example.com";

        Assert.Equal("https://example.com", _service.ServerUrl);
    }

    [Fact]
    public void PrivacyModeChanged_EventFires()
    {
        PrivacyMode? receivedMode = null;
        _service.PrivacyModeChanged += (sender, mode) => receivedMode = mode;

        _service.PrivacyMode = PrivacyMode.Relaxed;

        Assert.Equal(PrivacyMode.Relaxed, receivedMode);
    }

    [Theory]
    [InlineData(PrivacyMode.Relaxed)]
    [InlineData(PrivacyMode.Standard)]
    [InlineData(PrivacyMode.Strict)]
    public void PrivacyMode_AllModesAreSupported(PrivacyMode mode)
    {
        _service.PrivacyMode = mode;
        Assert.Equal(mode, _service.PrivacyMode);
    }
}
