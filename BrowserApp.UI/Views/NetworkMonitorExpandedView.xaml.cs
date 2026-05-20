using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using BrowserApp.Core.Models;
using BrowserApp.UI.ViewModels;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class NetworkMonitorExpandedView : FluentWindow
{
    private ICollectionView? _view;
    private NetworkMonitorViewModel? _vm;

    public NetworkMonitorExpandedView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NetworkMonitorViewModel vm) return;
        _vm = vm;

        // Wire up a default view filter over AllRequests so the search TextBox can narrow the
        // grid in-memory without thrashing the DB. The filter inspects SearchQuery on the VM,
        // so when the user types we refresh the view and recount visible rows.
        _view = CollectionViewSource.GetDefaultView(vm.AllRequests);
        _view.Filter = MatchesSearch;
        vm.PropertyChanged += OnVmPropertyChanged;
        vm.AllRequests.CollectionChanged += OnAllRequestsChanged;

        await vm.LoadAllRequestsCommand.ExecuteAsync(null);
        RefreshFiltered();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.AllRequests.CollectionChanged -= OnAllRequestsChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NetworkMonitorViewModel.SearchQuery))
        {
            RefreshFiltered();
        }
    }

    private void OnAllRequestsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // The default view auto-handles add/remove via INotifyCollectionChanged — we just need
        // to recount visible rows for the footer.
        Dispatcher.BeginInvoke(new Action(UpdateVisibleCount), System.Windows.Threading.DispatcherPriority.Background);
    }

    private bool MatchesSearch(object item)
    {
        if (_vm == null || string.IsNullOrWhiteSpace(_vm.SearchQuery)) return true;
        if (item is not NetworkRequest request) return false;
        var q = _vm.SearchQuery;
        return (request.Url?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
            || (request.Host?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void RefreshFiltered()
    {
        _view?.Refresh();
        UpdateVisibleCount();
    }

    private void UpdateVisibleCount()
    {
        if (_vm == null) return;
        if (_view is System.Windows.Data.CollectionView cv)
        {
            _vm.FilteredAllRequestsCount = cv.Count;
        }
        else
        {
            _vm.FilteredAllRequestsCount = _vm.AllRequests.Count;
        }
    }
}
