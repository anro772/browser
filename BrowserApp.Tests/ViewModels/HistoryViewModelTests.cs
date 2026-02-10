using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

public class HistoryViewModelTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IBrowsingHistoryRepository> _repositoryMock;
    private readonly TabStripViewModel _tabStrip;
    private readonly HistoryViewModel _viewModel;

    public HistoryViewModelTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _repositoryMock = new Mock<IBrowsingHistoryRepository>();

        // Create a real TabStripViewModel with mocked dependencies
        var blockingServiceMock = new Mock<IBlockingService>();
        var ruleEngineMock = new Mock<IRuleEngine>();
        var searchEngineMock = new Mock<ISearchEngineService>();
        _tabStrip = new TabStripViewModel(
            blockingServiceMock.Object,
            ruleEngineMock.Object,
            searchEngineMock.Object);

        // Setup the scope factory chain
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBrowsingHistoryRepository)))
            .Returns(_repositoryMock.Object);

        _viewModel = new HistoryViewModel(_scopeFactoryMock.Object, _tabStrip);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        Assert.Empty(_viewModel.HistoryEntries);
        Assert.Equal(string.Empty, _viewModel.SearchQuery);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.SelectedEntry);
    }

    [Fact]
    public async Task LoadHistoryAsync_CallsRepository()
    {
        var testEntries = new List<BrowsingHistoryEntity>
        {
            new() { Id = 1, Url = "https://google.com", Title = "Google", VisitedAt = DateTime.Now },
            new() { Id = 2, Url = "https://github.com", Title = "GitHub", VisitedAt = DateTime.Now.AddMinutes(-1) }
        };

        _repositoryMock.Setup(x => x.GetRecentAsync(200))
            .ReturnsAsync(testEntries);

        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Verify repository was called
        // Note: UI updates via Dispatcher don't work in unit tests (no WPF context)
        _repositoryMock.Verify(x => x.GetRecentAsync(200), Times.Once);
    }

    [Fact]
    public async Task SearchHistoryAsync_WithQuery_CallsSearchAsync()
    {
        var searchResults = new List<BrowsingHistoryEntity>
        {
            new() { Id = 1, Url = "https://github.com", Title = "GitHub", VisitedAt = DateTime.Now }
        };

        _repositoryMock.Setup(x => x.SearchAsync("github"))
            .ReturnsAsync(searchResults);

        // Call search command directly instead of relying on property change
        _viewModel.SearchQuery = "github";
        await _viewModel.SearchHistoryCommand.ExecuteAsync(null);

        _repositoryMock.Verify(x => x.SearchAsync("github"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SearchHistoryAsync_WithEmptyQuery_CallsGetRecentAsync()
    {
        _repositoryMock.Setup(x => x.GetRecentAsync(200))
            .ReturnsAsync(new List<BrowsingHistoryEntity>());

        _viewModel.SearchQuery = "";
        await _viewModel.SearchHistoryCommand.ExecuteAsync(null);

        _repositoryMock.Verify(x => x.GetRecentAsync(200), Times.AtLeastOnce);
    }

    [Fact]
    public async Task NavigateToEntryAsync_WithNullEntry_DoesNotThrow()
    {
        // With no active tab, navigating should be a no-op
        await _viewModel.NavigateToEntryCommand.ExecuteAsync(null);
        // No exception = pass
    }

    [Fact]
    public async Task LoadHistoryAsync_SetsIsLoadingCorrectly()
    {
        _repositoryMock.Setup(x => x.GetRecentAsync(200))
            .ReturnsAsync(new List<BrowsingHistoryEntity>());

        var loadingStates = new List<bool>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsLoading))
                loadingStates.Add(_viewModel.IsLoading);
        };

        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        Assert.Contains(true, loadingStates);
        Assert.False(_viewModel.IsLoading); // Should be false at the end
    }
}
