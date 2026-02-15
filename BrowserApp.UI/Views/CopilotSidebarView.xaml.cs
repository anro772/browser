using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class CopilotSidebarView : UserControl
{
    private readonly CopilotSidebarViewModel _viewModel;
    private ChatMessageItem? _subscribedStreamingItem;

    public CopilotSidebarView(CopilotSidebarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Auto-scroll when new messages arrive
        _viewModel.Messages.CollectionChanged += OnMessagesChanged;

        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.CheckConnectionCommand.ExecuteAsync(null);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromStreamingItem();
        _viewModel.Messages.CollectionChanged -= OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to bottom when messages are added
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatScrollViewer.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // Only subscribe to the last streaming message
        if (e.NewItems != null)
        {
            foreach (ChatMessageItem item in e.NewItems)
            {
                if (item.IsStreaming)
                {
                    UnsubscribeFromStreamingItem();
                    _subscribedStreamingItem = item;
                    item.PropertyChanged += OnStreamingItemPropertyChanged;
                }
            }
        }
    }

    private void OnStreamingItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ChatMessageItem.Content))
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatScrollViewer.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        else if (args.PropertyName == nameof(ChatMessageItem.IsStreaming))
        {
            var item = sender as ChatMessageItem;
            if (item != null && !item.IsStreaming)
            {
                UnsubscribeFromStreamingItem();
            }
        }
    }

    private void UnsubscribeFromStreamingItem()
    {
        if (_subscribedStreamingItem != null)
        {
            _subscribedStreamingItem.PropertyChanged -= OnStreamingItemPropertyChanged;
            _subscribedStreamingItem = null;
        }
    }

    private void ChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            if (_viewModel.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}
