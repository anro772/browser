using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class CopilotSidebarView : UserControl
{
    private readonly CopilotSidebarViewModel _viewModel;

    public CopilotSidebarView(CopilotSidebarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Auto-scroll when new messages arrive
        _viewModel.Messages.CollectionChanged += OnMessagesChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.CheckConnectionCommand.ExecuteAsync(null);
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

        // Also listen for content changes on the last message (streaming)
        if (e.NewItems != null)
        {
            foreach (ChatMessageItem item in e.NewItems)
            {
                item.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ChatMessageItem.Content))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ChatScrollViewer.ScrollToEnd();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                };
            }
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
