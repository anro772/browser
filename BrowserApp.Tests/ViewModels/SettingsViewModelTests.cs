using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IBrowsingHistoryRepository> _historyRepositoryMock;
    private readonly Mock<INetworkLogRepository> _networkLogRepositoryMock;
    private readonly Mock<ISearchEngineService> _searchEngineServiceMock;
    private readonly ExtensionService _extensionService;
    private readonly SettingsService _settingsService;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _historyRepositoryMock = new Mock<IBrowsingHistoryRepository>();
        _networkLogRepositoryMock = new Mock<INetworkLogRepository>();
        _searchEngineServiceMock = new Mock<ISearchEngineService>();
        _settingsService = new SettingsService();
        _extensionService = new ExtensionService(_scopeFactoryMock.Object);

        // Setup the scope factory chain
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBrowsingHistoryRepository)))
            .Returns(_historyRepositoryMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(INetworkLogRepository)))
            .Returns(_networkLogRepositoryMock.Object);

        _searchEngineServiceMock.Setup(x => x.AvailableEngines).Returns(new List<string> { "Google", "Bing", "Custom" });

        _viewModel = new SettingsViewModel(_settingsService, _scopeFactoryMock.Object, _searchEngineServiceMock.Object, _extensionService);
    }

    [Fact]
    public void DefaultValues_LoadFromSettingsService()
    {
        // Settings may have persisted values, just verify they match the service
        Assert.Equal(_settingsService.PrivacyMode, _viewModel.SelectedPrivacyMode);
        Assert.Equal(_settingsService.ServerUrl, _viewModel.ServerUrl);
        Assert.False(_viewModel.IsSaving);
    }

    [Fact]
    public void PrivacyModes_ContainsAllThreeModes()
    {
        Assert.Equal(3, _viewModel.PrivacyModes.Count);
        Assert.Contains(_viewModel.PrivacyModes, m => m.Mode == PrivacyMode.Relaxed);
        Assert.Contains(_viewModel.PrivacyModes, m => m.Mode == PrivacyMode.Standard);
        Assert.Contains(_viewModel.PrivacyModes, m => m.Mode == PrivacyMode.Strict);
    }

    [Fact]
    public void SelectedPrivacyMode_UpdatesSettingsService()
    {
        _viewModel.SelectedPrivacyMode = PrivacyMode.Strict;

        Assert.Equal(PrivacyMode.Strict, _settingsService.PrivacyMode);
    }

    [Fact]
    public void ServerUrl_UpdatesSettingsService()
    {
        _viewModel.ServerUrl = "https://new-server.com";

        Assert.Equal("https://new-server.com", _settingsService.ServerUrl);
    }

    [Fact]
    public void Settings_AutoSaveOnPropertyChange()
    {
        // SaveSettings command was removed — auto-save via OnPropertyChanged is the only path now.
        _viewModel.SelectedPrivacyMode = PrivacyMode.Relaxed;
        _viewModel.ServerUrl = "https://custom.com";

        Assert.Equal(PrivacyMode.Relaxed, _settingsService.PrivacyMode);
        Assert.Equal("https://custom.com", _settingsService.ServerUrl);
    }

    [Theory]
    [InlineData(PrivacyMode.Relaxed, "Relaxed", "Minimal blocking - best for sites that break with aggressive blocking")]
    [InlineData(PrivacyMode.Standard, "Standard", "Balanced blocking - recommended for daily browsing")]
    [InlineData(PrivacyMode.Strict, "Strict", "Maximum blocking - may break some site functionality")]
    public void PrivacyModeOption_HasCorrectProperties(PrivacyMode mode, string name, string description)
    {
        var option = _viewModel.PrivacyModes.First(m => m.Mode == mode);

        Assert.Equal(name, option.Name);
        Assert.Equal(description, option.Description);
    }
}

/// <summary>
/// QOL additions to SettingsViewModel: URL validation, saved-counter pulse,
/// ConfirmDialog flow for destructive clear actions.
/// </summary>
public class SettingsViewModelValidationTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ISearchEngineService> _searchEngineServiceMock = new();
    private readonly ExtensionService _extensionService;
    private readonly SettingsService _settingsService = new();
    private readonly SettingsViewModel _vm;

    public SettingsViewModelValidationTests()
    {
        _extensionService = new ExtensionService(_scopeFactoryMock.Object);
        _searchEngineServiceMock.Setup(x => x.AvailableEngines).Returns(new List<string> { "Google", "Custom" });
        _vm = new SettingsViewModel(_settingsService, _scopeFactoryMock.Object, _searchEngineServiceMock.Object, _extensionService);
    }

    // ─── HomePage validation ───

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("")]                                  // empty = "use default", allowed
    public void HomePage_ValidUrl_ClearsError(string url)
    {
        _vm.HomePage = url;
        Assert.Equal(string.Empty, _vm.HomePageError);
        Assert.Equal(url, _settingsService.HomePage);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://nope.com")]
    [InlineData("about:blank")]
    public void HomePage_InvalidUrl_SetsErrorAndDoesNotPersist(string url)
    {
        var previous = _settingsService.HomePage;
        _vm.HomePage = url;
        Assert.NotEqual(string.Empty, _vm.HomePageError);
        Assert.Equal(previous, _settingsService.HomePage);
    }

    [Fact]
    public void HomePage_FixingInvalid_ClearsError()
    {
        _vm.HomePage = "bogus";
        Assert.NotEqual(string.Empty, _vm.HomePageError);

        _vm.HomePage = "https://example.com";
        Assert.Equal(string.Empty, _vm.HomePageError);
    }

    // ─── ServerUrl validation ───

    [Fact]
    public void ServerUrl_InvalidUrl_SetsErrorAndDoesNotPersist()
    {
        var previous = _settingsService.ServerUrl;
        _vm.ServerUrl = "not a url";
        Assert.NotEqual(string.Empty, _vm.ServerUrlError);
        Assert.Equal(previous, _settingsService.ServerUrl);
    }

    [Fact]
    public void ServerUrl_ValidUrl_ClearsErrorAndPersists()
    {
        _vm.ServerUrl = "https://api.example.com";
        Assert.Equal(string.Empty, _vm.ServerUrlError);
        Assert.Equal("https://api.example.com", _settingsService.ServerUrl);
    }

    // ─── Custom search engine validation ───

    [Fact]
    public void CustomSearchEngine_WithPlaceholder_IsAccepted()
    {
        _vm.CustomSearchEngineUrl = "https://search.example.com/?q={query}";
        Assert.Equal(string.Empty, _vm.CustomSearchEngineError);
    }

    [Fact]
    public void CustomSearchEngine_NotAUrl_Rejected()
    {
        _vm.CustomSearchEngineUrl = "definitely not a url";
        Assert.NotEqual(string.Empty, _vm.CustomSearchEngineError);
    }

    // ─── LastSavedCounter pulse ───

    [Fact]
    public void LastSavedCounter_IncrementsOnAutoSave()
    {
        // Pick a value distinct from whatever was loaded from the persisted service —
        // [ObservableProperty] only fires PropertyChanged on actual changes, so an
        // assignment to the same value would no-op.
        var different = _vm.SelectedPrivacyMode == PrivacyMode.Strict
            ? PrivacyMode.Relaxed
            : PrivacyMode.Strict;

        var before = _vm.LastSavedCounter;
        _vm.SelectedPrivacyMode = different;
        Assert.True(_vm.LastSavedCounter > before);
    }

    [Fact]
    public void LastSavedCounter_IncrementsForEachField()
    {
        var c0 = _vm.LastSavedCounter;

        // Use values guaranteed to differ from the loaded baseline
        _vm.HomePage = _vm.HomePage == "https://a.com" ? "https://b.com" : "https://a.com";
        var c1 = _vm.LastSavedCounter;

        _vm.SelectedStartupBehavior = _vm.SelectedStartupBehavior == StartupBehavior.NewTab
            ? StartupBehavior.RestoreSession
            : StartupBehavior.NewTab;
        var c2 = _vm.LastSavedCounter;

        _vm.ShowBookmarksBar = !_vm.ShowBookmarksBar;
        var c3 = _vm.LastSavedCounter;

        Assert.True(c1 > c0, $"HomePage change should bump counter ({c0} → {c1})");
        Assert.True(c2 > c1, $"StartupBehavior change should bump counter ({c1} → {c2})");
        Assert.True(c3 > c2, $"ShowBookmarksBar change should bump counter ({c2} → {c3})");
    }

    [Fact]
    public void LastSavedCounter_DoesNotPulseOnInvalidUrl()
    {
        var before = _vm.LastSavedCounter;
        _vm.HomePage = "not a url";
        Assert.Equal(before, _vm.LastSavedCounter);
    }

    // ─── SaveSettings command removal ───

    [Fact]
    public void SaveSettings_CommandWasRemoved()
    {
        // The unused SaveSettings command was deleted as part of consolidating
        // to auto-save-on-change. If someone re-adds it, this test fires so we
        // re-evaluate whether the duplicate save path is needed.
        var commandProp = typeof(SettingsViewModel).GetProperty("SaveSettingsCommand");
        Assert.Null(commandProp);
    }
}

public class PrivacyModeOptionTests
{
    [Fact]
    public void Record_CanBeCreated()
    {
        var option = new PrivacyModeOption(PrivacyMode.Standard, "Standard", "Description");

        Assert.Equal(PrivacyMode.Standard, option.Mode);
        Assert.Equal("Standard", option.Name);
        Assert.Equal("Description", option.Description);
    }

    [Fact]
    public void Record_SupportsEquality()
    {
        var option1 = new PrivacyModeOption(PrivacyMode.Standard, "Standard", "Description");
        var option2 = new PrivacyModeOption(PrivacyMode.Standard, "Standard", "Description");

        Assert.Equal(option1, option2);
    }
}
