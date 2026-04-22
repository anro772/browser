using System.Windows;
using System.Windows.Input;
using BrowserApp.UI.ViewModels;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class QuickRuleWizardDialog : FluentWindow
{
    private readonly QuickRuleWizardViewModel _viewModel;

    public bool WasSaved => _viewModel.WasSaved;
    public bool OpenAdvancedEditor => _viewModel.OpenAdvancedEditor;

    public QuickRuleWizardDialog(QuickRuleWizardViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.CloseAction = () =>
        {
            DialogResult = _viewModel.WasSaved || _viewModel.OpenAdvancedEditor;
            Close();
        };

        DataContext = _viewModel;
        InitializeComponent();
    }

    private void ShowStep1(string type)
    {
        _viewModel.SelectTypeCommand.Execute(type);

        if (type == "advanced") return;

        StepTitleText.Text = _viewModel.StepTitle;
        StepDescriptionText.Text = _viewModel.StepDescription;
        DescriptionPanel.Visibility = _viewModel.ShowDescriptionField
            ? Visibility.Visible : Visibility.Collapsed;

        Step0Panel.Visibility = Visibility.Collapsed;
        Step1Panel.Visibility = Visibility.Visible;
        DomainInput.Focus();
    }

    private void BlockDomain_Click(object sender, MouseButtonEventArgs e) => ShowStep1("block_domain");
    private void BlockAds_Click(object sender, MouseButtonEventArgs e) => ShowStep1("block_ads");
    private void HideElement_Click(object sender, MouseButtonEventArgs e) => ShowStep1("hide_element");
    private void Advanced_Click(object sender, MouseButtonEventArgs e) => ShowStep1("advanced");

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.GoBackCommand.Execute(null);
        Step1Panel.Visibility = Visibility.Collapsed;
        Step0Panel.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CreateRuleCommand.ExecuteAsync(null);

        if (_viewModel.ErrorMessage != null)
        {
            ErrorText.Text = _viewModel.ErrorMessage;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
