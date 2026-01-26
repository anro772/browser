using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

public class PrivacyDashboardViewModelTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<INetworkLogRepository> _repositoryMock;
    private readonly PrivacyDashboardViewModel _viewModel;

    public PrivacyDashboardViewModelTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _repositoryMock = new Mock<INetworkLogRepository>();

        // Setup the scope factory chain
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(INetworkLogRepository)))
            .Returns(_repositoryMock.Object);

        _viewModel = new PrivacyDashboardViewModel(_scopeFactoryMock.Object);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        Assert.Equal(PrivacyMode.Standard, _viewModel.CurrentPrivacyMode);
        Assert.Equal("0 B", _viewModel.DataSaved);
        Assert.Equal(0, _viewModel.BlockedToday);
        Assert.Equal(0, _viewModel.TotalBlocked);
        Assert.Empty(_viewModel.TopBlockedDomains);
        Assert.Empty(_viewModel.ResourceTypeBreakdown);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public void PrivacyModeDisplay_ReturnsCorrectString()
    {
        _viewModel.CurrentPrivacyMode = PrivacyMode.Relaxed;
        Assert.Equal("Relaxed", _viewModel.PrivacyModeDisplay);

        _viewModel.CurrentPrivacyMode = PrivacyMode.Standard;
        Assert.Equal("Standard", _viewModel.PrivacyModeDisplay);

        _viewModel.CurrentPrivacyMode = PrivacyMode.Strict;
        Assert.Equal("Strict", _viewModel.PrivacyModeDisplay);
    }

    [Fact]
    public void PrivacyModeColor_ReturnsCorrectColor()
    {
        _viewModel.CurrentPrivacyMode = PrivacyMode.Relaxed;
        Assert.Equal("#FFC107", _viewModel.PrivacyModeColor);

        _viewModel.CurrentPrivacyMode = PrivacyMode.Standard;
        Assert.Equal("#107C10", _viewModel.PrivacyModeColor);

        _viewModel.CurrentPrivacyMode = PrivacyMode.Strict;
        Assert.Equal("#D13438", _viewModel.PrivacyModeColor);
    }

    [Fact]
    public async Task RefreshStatsAsync_CallsRepositoryMethods()
    {
        // Setup repository mock responses
        _repositoryMock.Setup(x => x.GetBlockedTodayCountAsync())
            .ReturnsAsync(42);
        _repositoryMock.Setup(x => x.GetBlockedCountAsync())
            .ReturnsAsync(100);
        _repositoryMock.Setup(x => x.GetTotalSizeAsync())
            .ReturnsAsync(1024 * 1024); // 1 MB
        _repositoryMock.Setup(x => x.GetTopBlockedDomainsAsync(5))
            .ReturnsAsync(new List<(string, int)> { ("tracker.com", 50), ("ads.com", 30) });
        _repositoryMock.Setup(x => x.GetResourceTypeBreakdownAsync())
            .ReturnsAsync(new List<(string, int)> { ("Script", 80), ("Image", 20) });

        await _viewModel.RefreshStatsCommand.ExecuteAsync(null);

        // Verify all repository methods were called
        // Note: UI updates via Dispatcher don't work in unit tests (no WPF context)
        _repositoryMock.Verify(x => x.GetBlockedTodayCountAsync(), Times.Once);
        _repositoryMock.Verify(x => x.GetBlockedCountAsync(), Times.Once);
        _repositoryMock.Verify(x => x.GetTotalSizeAsync(), Times.Once);
        _repositoryMock.Verify(x => x.GetTopBlockedDomainsAsync(5), Times.Once);
        _repositoryMock.Verify(x => x.GetResourceTypeBreakdownAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshStatsAsync_SetsIsLoadingDuringOperation()
    {
        var loadingStates = new List<bool>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsLoading))
                loadingStates.Add(_viewModel.IsLoading);
        };

        // Setup repository to return empty data
        _repositoryMock.Setup(x => x.GetBlockedTodayCountAsync()).ReturnsAsync(0);
        _repositoryMock.Setup(x => x.GetBlockedCountAsync()).ReturnsAsync(0);
        _repositoryMock.Setup(x => x.GetTotalSizeAsync()).ReturnsAsync(0L);
        _repositoryMock.Setup(x => x.GetTopBlockedDomainsAsync(5))
            .ReturnsAsync(new List<(string, int)>());
        _repositoryMock.Setup(x => x.GetResourceTypeBreakdownAsync())
            .ReturnsAsync(new List<(string, int)>());

        await _viewModel.RefreshStatsCommand.ExecuteAsync(null);

        // Should have been set to true then false
        Assert.Contains(true, loadingStates);
        Assert.False(_viewModel.IsLoading); // Should be false at the end
    }

    [Fact]
    public async Task RefreshStatsAsync_HandlesEmptyData()
    {
        _repositoryMock.Setup(x => x.GetBlockedTodayCountAsync()).ReturnsAsync(0);
        _repositoryMock.Setup(x => x.GetBlockedCountAsync()).ReturnsAsync(0);
        _repositoryMock.Setup(x => x.GetTotalSizeAsync()).ReturnsAsync(0L);
        _repositoryMock.Setup(x => x.GetTopBlockedDomainsAsync(5))
            .ReturnsAsync(new List<(string, int)>());
        _repositoryMock.Setup(x => x.GetResourceTypeBreakdownAsync())
            .ReturnsAsync(new List<(string, int)>());

        await _viewModel.RefreshStatsCommand.ExecuteAsync(null);

        Assert.Equal(0, _viewModel.BlockedToday);
        Assert.Empty(_viewModel.TopBlockedDomains);
        Assert.Empty(_viewModel.ResourceTypeBreakdown);
    }
}

public class BlockedDomainItemTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var item = new BlockedDomainItem
        {
            Domain = "example.com",
            Count = 42,
            Percentage = 75.5
        };

        Assert.Equal("example.com", item.Domain);
        Assert.Equal(42, item.Count);
        Assert.Equal(75.5, item.Percentage);
    }
}

public class ResourceTypeItemTests
{
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var item = new ResourceTypeItem
        {
            Type = "Script",
            Count = 100,
            Percentage = 50.0
        };

        Assert.Equal("Script", item.Type);
        Assert.Equal(100, item.Count);
        Assert.Equal(50.0, item.Percentage);
    }
}
