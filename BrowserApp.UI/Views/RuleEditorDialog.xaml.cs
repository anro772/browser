using System.Windows;
using BrowserApp.UI.ViewModels;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class RuleEditorDialog : FluentWindow
{
    public RuleEditorDialog(RuleEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = () =>
        {
            try { DialogResult = viewModel.WasSaved; }
            catch (InvalidOperationException) { }
            Close();
        };
    }
}
