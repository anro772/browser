using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Controls;

/// <summary>
/// Reusable empty-state slab: icon + eyebrow + headline + body + up-to-two CTAs.
/// One layout, varied per workspace via the dependency properties — matches the
/// Claude Design recommendation (empty.jsx).
///
/// Wire CTAs by subscribing to PrimaryCtaClicked / SecondaryCtaClicked.
/// </summary>
public partial class WorkspaceEmptyState : UserControl
{
    public WorkspaceEmptyState()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IconSymbolProperty =
        DependencyProperty.Register(nameof(IconSymbol), typeof(SymbolRegular), typeof(WorkspaceEmptyState),
            new PropertyMetadata(SymbolRegular.Sparkle24));

    public SymbolRegular IconSymbol
    {
        get => (SymbolRegular)GetValue(IconSymbolProperty);
        set => SetValue(IconSymbolProperty, value);
    }

    public static readonly DependencyProperty EyebrowProperty =
        DependencyProperty.Register(nameof(Eyebrow), typeof(string), typeof(WorkspaceEmptyState),
            new PropertyMetadata("GET STARTED"));

    public string Eyebrow
    {
        get => (string)GetValue(EyebrowProperty);
        set => SetValue(EyebrowProperty, value);
    }

    public static readonly DependencyProperty EyebrowBrushProperty =
        DependencyProperty.Register(nameof(EyebrowBrush), typeof(Brush), typeof(WorkspaceEmptyState),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x7C, 0x6A, 0xEF))));

    public Brush EyebrowBrush
    {
        get => (Brush)GetValue(EyebrowBrushProperty);
        set => SetValue(EyebrowBrushProperty, value);
    }

    public static readonly DependencyProperty EyebrowSubtleBrushProperty =
        DependencyProperty.Register(nameof(EyebrowSubtleBrush), typeof(Brush), typeof(WorkspaceEmptyState),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x22, 0x7C, 0x6A, 0xEF))));

    public Brush EyebrowSubtleBrush
    {
        get => (Brush)GetValue(EyebrowSubtleBrushProperty);
        set => SetValue(EyebrowSubtleBrushProperty, value);
    }

    public static readonly DependencyProperty HeadlineProperty =
        DependencyProperty.Register(nameof(Headline), typeof(string), typeof(WorkspaceEmptyState),
            new PropertyMetadata(""));

    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(nameof(Body), typeof(string), typeof(WorkspaceEmptyState),
            new PropertyMetadata(""));

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public static readonly DependencyProperty PrimaryCtaTextProperty =
        DependencyProperty.Register(nameof(PrimaryCtaText), typeof(string), typeof(WorkspaceEmptyState),
            new PropertyMetadata("", OnCtaTextChanged));

    public string PrimaryCtaText
    {
        get => (string)GetValue(PrimaryCtaTextProperty);
        set => SetValue(PrimaryCtaTextProperty, value);
    }

    public static readonly DependencyProperty SecondaryCtaTextProperty =
        DependencyProperty.Register(nameof(SecondaryCtaText), typeof(string), typeof(WorkspaceEmptyState),
            new PropertyMetadata("", OnCtaTextChanged));

    public string SecondaryCtaText
    {
        get => (string)GetValue(SecondaryCtaTextProperty);
        set => SetValue(SecondaryCtaTextProperty, value);
    }

    public static readonly DependencyProperty HasPrimaryCtaProperty =
        DependencyProperty.Register(nameof(HasPrimaryCta), typeof(bool), typeof(WorkspaceEmptyState),
            new PropertyMetadata(false));

    public bool HasPrimaryCta
    {
        get => (bool)GetValue(HasPrimaryCtaProperty);
        private set => SetValue(HasPrimaryCtaProperty, value);
    }

    public static readonly DependencyProperty HasSecondaryCtaProperty =
        DependencyProperty.Register(nameof(HasSecondaryCta), typeof(bool), typeof(WorkspaceEmptyState),
            new PropertyMetadata(false));

    public bool HasSecondaryCta
    {
        get => (bool)GetValue(HasSecondaryCtaProperty);
        private set => SetValue(HasSecondaryCtaProperty, value);
    }

    private static void OnCtaTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WorkspaceEmptyState es) return;
        es.HasPrimaryCta = !string.IsNullOrEmpty(es.PrimaryCtaText);
        es.HasSecondaryCta = !string.IsNullOrEmpty(es.SecondaryCtaText);
    }

    public event EventHandler? PrimaryCtaClicked;
    public event EventHandler? SecondaryCtaClicked;

    private void PrimaryCta_Click(object sender, RoutedEventArgs e)
        => PrimaryCtaClicked?.Invoke(this, EventArgs.Empty);

    private void SecondaryCta_Click(object sender, RoutedEventArgs e)
        => SecondaryCtaClicked?.Invoke(this, EventArgs.Empty);
}
