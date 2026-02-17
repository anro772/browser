using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Models;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BrowserApp.Tests.ViewModels;

public class MainViewModelSidebarTests
{
    [Fact]
    public void SelectedSidebarSection_DefaultsToCopilot()
    {
        var vm = CreateViewModel();

        Assert.Equal(SidebarSection.Copilot, vm.SelectedSidebarSection);
    }

    [Fact]
    public void SelectedSidebarSection_CanBeChanged()
    {
        var vm = CreateViewModel();

        vm.SelectedSidebarSection = SidebarSection.Downloads;

        Assert.Equal(SidebarSection.Downloads, vm.SelectedSidebarSection);
    }

    private static MainViewModel CreateViewModel()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var searchEngineService = new Mock<ISearchEngineService>();
        var historyRepository = new Mock<BrowserApp.Data.Interfaces.IBrowsingHistoryRepository>();
        var blockingService = new Mock<IBlockingService>();
        var ruleEngine = new Mock<IRuleEngine>();

        var tabStrip = new TabStripViewModel(
            blockingService.Object,
            ruleEngine.Object,
            searchEngineService.Object);

        var bookmarkVm = new BookmarkViewModel(scopeFactory.Object, tabStrip);

        return new MainViewModel(
            searchEngineService.Object,
            historyRepository.Object,
            scopeFactory.Object,
            tabStrip,
            bookmarkVm);
    }
}
