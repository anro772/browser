using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Maps a channel name (or id) to one of the design palette hues via a stable hash,
/// so each channel gets a consistent color across avatars, name labels, and stripes.
/// Prefer binding on Name (user-visible, more entropy than Guid prefixes for short inputs).
/// Mirrors the PALETTE in browser-profiles/channels.jsx, widened to 16 hues to reduce
/// collision probability across small channel lists.
/// </summary>
public class ChannelIdToBrushConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.FromRgb(0x7C, 0x6A, 0xEF), // indigo
        Color.FromRgb(0x5E, 0xEA, 0xD4), // teal
        Color.FromRgb(0xF0, 0x70, 0x8A), // rose
        Color.FromRgb(0xFA, 0xCC, 0x15), // amber
        Color.FromRgb(0xA7, 0x8B, 0xFA), // lavender
        Color.FromRgb(0x60, 0xA5, 0xFA), // sky
        Color.FromRgb(0x34, 0xD3, 0x99), // emerald
        Color.FromRgb(0xFB, 0x71, 0x85), // coral
        Color.FromRgb(0xF4, 0xA4, 0x7A), // peach
        Color.FromRgb(0x7D, 0xD3, 0xC0), // mint
        Color.FromRgb(0xC4, 0xB5, 0xFD), // lilac
        Color.FromRgb(0xFC, 0xA5, 0xA5), // salmon
        Color.FromRgb(0x6E, 0xE7, 0xB7), // jade
        Color.FromRgb(0xFD, 0xBA, 0x74), // tangerine
        Color.FromRgb(0x93, 0xC5, 0xFD), // periwinkle
        Color.FromRgb(0xF0, 0xAB, 0xFC), // orchid
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = Pick(value);
        return new SolidColorBrush(color) { Opacity = ParseAlpha(parameter) };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Color Pick(object? value)
    {
        var s = value?.ToString();
        if (string.IsNullOrEmpty(s))
        {
            return Palette[0];
        }
        // FNV-1a 32-bit — better avalanche than `h * 31 + c` for short ASCII inputs
        // like "1111", "2222", "aaaa", so similar-looking names get distinct hues.
        const uint offsetBasis = 2166136261u;
        const uint prime = 16777619u;
        var h = offsetBasis;
        for (var i = 0; i < s.Length; i++)
        {
            h ^= s[i];
            h = unchecked(h * prime);
        }
        return Palette[h % (uint)Palette.Length];
    }

    private static double ParseAlpha(object? parameter)
    {
        if (parameter is string p && double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return Math.Clamp(d, 0.0, 1.0);
        }
        return 1.0;
    }
}
