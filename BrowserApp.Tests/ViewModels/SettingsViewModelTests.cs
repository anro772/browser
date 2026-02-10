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

        // Setup the scope factory chain
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBrowsingHistoryRepository)))
            .Returns(_historyRepositoryMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(INetworkLogRepository)))
            .Returns(_networkLogRepositoryMock.Object);

        _searchEngineServiceMock.Setup(x => x.AvailableEngines).Returns(new List<string> { "Google", "Bing", "Custom" });

        _viewModel = new SettingsViewModel(_settingsService, _scopeFactoryMock.Object, _searchEngineServiceMock.Object);
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
    public void SaveSettingsCommand_UpdatesSettingsService()
    {
        _viewModel.SelectedPrivacyMode = PrivacyMode.Relaxed;
        _viewModel.ServerUrl = "https://custom.com";

        _viewModel.SaveSettingsCommand.Execute(null);

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
