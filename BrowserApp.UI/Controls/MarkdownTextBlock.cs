using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrowserApp.UI.Controls;

/// Lightweight, streaming-safe Markdown renderer for the Copilot bubble.
/// Re-parses and rebuilds the visual tree on every Markdown change — message
/// bodies are short, so the O(n) cost is invisible in practice.
public class MarkdownTextBlock : ContentControl
{
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown), typeof(string), typeof(MarkdownTextBlock),
        new PropertyMetadata(string.Empty, OnMarkdownChanged));

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MarkdownTextBlock)d).Rebuild((string)e.NewValue ?? string.Empty);
    }

    private void Rebuild(string source)
    {
        var root = new StackPanel();
        var blocks = ParseBlocks(source);

        for (int i = 0; i < blocks.Count; i++)
        {
            var element = RenderBlock(blocks[i], firstBlock: i == 0);
            if (element != null) root.Children.Add(element);
        }

        Content = root;
    }

    // ─────────────────────── Block parser ───────────────────────

    private enum BlockKind { Paragraph, Heading, Bullets, Numbered, Code }

    private class Block
    {
        public BlockKind Kind;
        public int HeadingLevel;          // 1..3 for Heading
        public string? CodeLanguage;      // for Code
        public List<string> Lines = new();
    }

    private static List<Block> ParseBlocks(string source)
    {
        var result = new List<Block>();
        if (string.IsNullOrEmpty(source)) return result;

        var lines = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        Block? currentList = null;
        Block? currentParagraph = null;
        Block? currentCode = null;

        void FlushList() { if (currentList != null) { result.Add(currentList); currentList = null; } }
        void FlushParagraph() { if (currentParagraph != null) { result.Add(currentParagraph); currentParagraph = null; } }

        for (int idx = 0; idx < lines.Length; idx++)
        {
            var raw = lines[idx];
            var trimmed = raw.TrimStart();

            // Inside a code fence — collect lines verbatim until the closing fence.
            if (currentCode != null)
            {
                if (trimmed.StartsWith("```"))
                {
                    result.Add(currentCode);
                    currentCode = null;
                }
                else
                {
                    currentCode.Lines.Add(raw);
                }
                continue;
            }

            // Opening code fence.
            if (trimmed.StartsWith("```"))
            {
                FlushList();
                FlushParagraph();
                currentCode = new Block
                {
                    Kind = BlockKind.Code,
                    CodeLanguage = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : null
                };
                continue;
            }

            // Blank line — break paragraph and lists.
            if (string.IsNullOrWhiteSpace(raw))
            {
                FlushList();
                FlushParagraph();
                continue;
            }

            // Headings.
            int hashes = 0;
            while (hashes < trimmed.Length && trimmed[hashes] == '#' && hashes < 6) hashes++;
            if (hashes > 0 && hashes <= 3 && hashes < trimmed.Length && trimmed[hashes] == ' ')
            {
                FlushList();
                FlushParagraph();
                result.Add(new Block
                {
                    Kind = BlockKind.Heading,
                    HeadingLevel = hashes,
                    Lines = { trimmed.Substring(hashes + 1).TrimEnd() }
                });
                continue;
            }

            // Bulleted list item: -, *, • prefix.
            if (TryStripBullet(trimmed, out var bulletText))
            {
                FlushParagraph();
                if (currentList?.Kind != BlockKind.Bullets)
                {
                    FlushList();
                    currentList = new Block { Kind = BlockKind.Bullets };
                }
                currentList.Lines.Add(bulletText);
                continue;
            }

            // Numbered list item: "1. ", "2) ", etc.
            if (TryStripNumber(trimmed, out var numberText))
            {
                FlushParagraph();
                if (currentList?.Kind != BlockKind.Numbered)
                {
                    FlushList();
                    currentList = new Block { Kind = BlockKind.Numbered };
                }
                currentList.Lines.Add(numberText);
                continue;
            }

            // Continuation of the current list item (indented under previous bullet).
            if (currentList != null && (raw.StartsWith("  ") || raw.StartsWith("\t")))
            {
                var lastIdx = currentList.Lines.Count - 1;
                if (lastIdx >= 0)
                {
                    currentList.Lines[lastIdx] = currentList.Lines[lastIdx] + " " + trimmed;
                    continue;
                }
            }

            // Paragraph line — append to current paragraph or start a new one.
            FlushList();
            if (currentParagraph == null) currentParagraph = new Block { Kind = BlockKind.Paragraph };
            currentParagraph.Lines.Add(trimmed);
        }

        // Flush trailing blocks. An unterminated code fence still renders as a code block.
        if (currentCode != null) result.Add(currentCode);
        FlushList();
        FlushParagraph();

        return result;
    }

    private static bool TryStripBullet(string trimmed, out string text)
    {
        if (trimmed.Length >= 2)
        {
            char c = trimmed[0];
            if ((c == '-' || c == '*' || c == '•') && trimmed[1] == ' ')
            {
                text = trimmed.Substring(2).TrimEnd();
                return true;
            }
        }
        text = string.Empty;
        return false;
    }

    private static bool TryStripNumber(string trimmed, out string text)
    {
        int i = 0;
        while (i < trimmed.Length && char.IsDigit(trimmed[i])) i++;
        if (i > 0 && i < trimmed.Length - 1 && (trimmed[i] == '.' || trimmed[i] == ')') && trimmed[i + 1] == ' ')
        {
            text = trimmed.Substring(i + 2).TrimEnd();
            return true;
        }
        text = string.Empty;
        return false;
    }

    // ─────────────────────── Block renderers ───────────────────────

    private UIElement? RenderBlock(Block block, bool firstBlock)
    {
        return block.Kind switch
        {
            BlockKind.Heading => RenderHeading(block, firstBlock),
            BlockKind.Paragraph => RenderParagraph(block, firstBlock),
            BlockKind.Bullets => RenderList(block, firstBlock, numbered: false),
            BlockKind.Numbered => RenderList(block, firstBlock, numbered: true),
            BlockKind.Code => RenderCodeBlock(block, firstBlock),
            _ => null
        };
    }

    private static TextBlock RenderHeading(Block block, bool firstBlock)
    {
        double size = block.HeadingLevel switch { 1 => 15.5, 2 => 14.5, _ => 13.5 };
        var tb = new TextBlock
        {
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
            Margin = new Thickness(0, firstBlock ? 0 : 10, 0, 4)
        };
        AppendInlines(tb.Inlines, block.Lines[0]);
        return tb;
    }

    private static TextBlock RenderParagraph(Block block, bool firstBlock)
    {
        var tb = new TextBlock
        {
            FontSize = 13,
            LineHeight = 20,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
            Margin = new Thickness(0, firstBlock ? 0 : 6, 0, 0)
        };
        AppendInlines(tb.Inlines, string.Join(" ", block.Lines));
        return tb;
    }

    private static UIElement RenderList(Block block, bool firstBlock, bool numbered)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, firstBlock ? 0 : 6, 0, 0)
        };

        for (int i = 0; i < block.Lines.Count; i++)
        {
            var row = new Grid { Margin = new Thickness(0, i == 0 ? 0 : 3, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(numbered ? 22 : 16) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            if (numbered)
            {
                var num = new TextBlock
                {
                    Text = (i + 1) + ".",
                    FontFamily = (FontFamily)Application.Current.Resources["AppMonoFontFamily"],
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 2, 8, 0)
                };
                Grid.SetColumn(num, 0);
                row.Children.Add(num);
            }
            else
            {
                var dot = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = (Brush)Application.Current.Resources["AccentLightBrush"],
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(2, 8, 0, 0)
                };
                Grid.SetColumn(dot, 0);
                row.Children.Add(dot);
            }

            var content = new TextBlock
            {
                FontSize = 13,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"]
            };
            AppendInlines(content.Inlines, block.Lines[i]);
            Grid.SetColumn(content, 1);
            row.Children.Add(content);

            panel.Children.Add(row);
        }
        return panel;
    }

    private static UIElement RenderCodeBlock(Block block, bool firstBlock)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderSubtleBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(11, 9, 11, 9),
            Margin = new Thickness(0, firstBlock ? 0 : 8, 0, 2)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var headerLeft = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(block.CodeLanguage) ? "code" : block.CodeLanguage,
            FontFamily = (FontFamily)Application.Current.Resources["AppMonoFontFamily"],
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
            Margin = new Thickness(0, 0, 0, 6),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(headerLeft, 0);
        Grid.SetColumn(headerLeft, 0);
        grid.Children.Add(headerLeft);

        var codeText = string.Join("\n", block.Lines);
        var copyButton = new Button
        {
            Content = "Copy",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(0, 0, 0, 6),
            Background = (Brush)Application.Current.Resources["BgSurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderSubtleBrush"],
            BorderThickness = new Thickness(1),
            Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        copyButton.Click += (_, _) =>
        {
            try { Clipboard.SetText(codeText); } catch { /* clipboard can fail transiently */ }
        };
        Grid.SetRow(copyButton, 0);
        Grid.SetColumn(copyButton, 1);
        grid.Children.Add(copyButton);

        var code = new TextBlock
        {
            Text = codeText,
            FontFamily = (FontFamily)Application.Current.Resources["AppMonoFontFamily"],
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.FromRgb(0xC7, 0xCC, 0xE0)),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(code, 1);
        Grid.SetColumn(code, 0);
        Grid.SetColumnSpan(code, 2);
        grid.Children.Add(code);

        border.Child = grid;
        return border;
    }

    // ─────────────────────── Inline parser ───────────────────────

    private static void AppendInlines(InlineCollection target, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var buffer = new StringBuilder();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];

            // Inline code: `...`
            if (c == '`')
            {
                int close = text.IndexOf('`', i + 1);
                if (close > i)
                {
                    FlushBuffer(target, buffer);
                    target.Add(MakeInlineCode(text.Substring(i + 1, close - i - 1)));
                    i = close + 1;
                    continue;
                }
            }

            // Bold: **...**
            if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int close = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (close > i + 1)
                {
                    FlushBuffer(target, buffer);
                    var inner = text.Substring(i + 2, close - i - 2);
                    target.Add(new Run(inner) { FontWeight = FontWeights.SemiBold });
                    i = close + 2;
                    continue;
                }
            }

            // Italic: *...* or _..._
            if ((c == '*' || c == '_') && i + 1 < text.Length && text[i + 1] != c)
            {
                int close = text.IndexOf(c, i + 1);
                if (close > i && close - i > 1)
                {
                    var inner = text.Substring(i + 1, close - i - 1);
                    if (!string.IsNullOrWhiteSpace(inner) && !inner.Contains('\n'))
                    {
                        FlushBuffer(target, buffer);
                        target.Add(new Run(inner) { FontStyle = FontStyles.Italic });
                        i = close + 1;
                        continue;
                    }
                }
            }

            buffer.Append(c);
            i++;
        }

        FlushBuffer(target, buffer);
    }

    private static void FlushBuffer(InlineCollection target, StringBuilder buffer)
    {
        if (buffer.Length == 0) return;
        target.Add(new Run(buffer.ToString()));
        buffer.Clear();
    }

    private static Inline MakeInlineCode(string text)
    {
        var run = new Run(" " + text + " ")
        {
            FontFamily = (FontFamily)Application.Current.Resources["AppMonoFontFamily"],
            FontSize = 12,
            Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
            Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"]
        };
        return run;
    }
}
