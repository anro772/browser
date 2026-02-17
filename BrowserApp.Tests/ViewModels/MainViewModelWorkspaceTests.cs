using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Models;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BrowserApp.Tests.ViewModels;

public class MainViewModelWorkspaceTests
{
    [Fact]
    public void Workspace_DefaultsToClosedAndNone()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsWorkspaceOpen);
        Assert.Equal(WorkspaceSection.None, vm.ActiveWorkspaceSection);
    }

    [Fact]
    public void OpenWorkspace_SetsSectionAndOpens()
    {
        var vm = CreateViewModel();

        vm.OpenWorkspaceCommand.Execute(WorkspaceSection.Rules);

        Assert.True(vm.IsWorkspaceOpen);
        Assert.Equal(WorkspaceSection.Rules, vm.ActiveWorkspaceSection);
    }

    [Fact]
    public void CloseWorkspace_ResetsStateWithoutAffectingSidebar()
    {
        var vm = CreateViewModel();
        vm.SelectedSidebarSection = SidebarSection.History;
        vm.OpenWorkspaceCommand.Execute(WorkspaceSection.Settings);

        vm.CloseWorkspaceCommand.Execute(null);

        Assert.False(vm.IsWorkspaceOpen);
        Assert.Equal(WorkspaceSection.None, vm.ActiveWorkspaceSection);
        Assert.Equal(SidebarSection.History, vm.SelectedSidebarSection);
    }

    private static MainViewModel CreateViewModel()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var searchEngineService = new Mock<ISearchEngineService>();
        var historyRepository = new Mock<IBrowsingHistoryRepository>();
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
