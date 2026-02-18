using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BrowserApp.UI.Converters;
using Xunit;

namespace BrowserApp.Tests.Converters;

public class BoolToVisibilityInverseConverterTests
{
    private readonly BoolToVisibilityInverseConverter _converter = new();

    [Theory]
    [InlineData(true, Visibility.Collapsed)]
    [InlineData(false, Visibility.Visible)]
    public void Convert_ReturnsCorrectVisibility(bool input, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsVisible()
    {
        var result = _converter.Convert("not a bool", typeof(Visibility), null!, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    [Theory]
    [InlineData(Visibility.Visible, false)]
    [InlineData(Visibility.Collapsed, true)]
    [InlineData(Visibility.Hidden, true)]
    public void ConvertBack_ReturnsCorrectBool(Visibility input, bool expected)
    {
        var result = _converter.ConvertBack(input, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }
}

public class CountToVisibilityConverterTests
{
    private readonly CountToVisibilityConverter _converter = new();

    [Theory]
    [InlineData(0, Visibility.Visible)]
    [InlineData(1, Visibility.Collapsed)]
    [InlineData(10, Visibility.Collapsed)]
    [InlineData(-1, Visibility.Collapsed)]
    public void Convert_ReturnsCorrectVisibility(int count, Visibility expected)
    {
        var result = _converter.Convert(count, typeof(Visibility), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonIntValue_ReturnsCollapsed()
    {
        var result = _converter.Convert("not an int", typeof(Visibility), null!, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Collapsed, result);
    }
}

public class PercentToWidthConverterTests
{
    private readonly PercentToWidthConverter _converter = new();

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(50.0, 75.0)]   // 50% of 150 base width
    [InlineData(100.0, 150.0)] // 100% of 150 base width
    public void Convert_ReturnsCorrectWidth(double percent, double expected)
    {
        var result = _converter.Convert(percent, typeof(double), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, (double)result, precision: 1);
    }

    [Theory]
    [InlineData(-10.0, 0.0)]   // Clamped to 0
    [InlineData(150.0, 150.0)] // Clamped to 100%
    public void Convert_ClampsOutOfRangeValues(double percent, double expected)
    {
        var result = _converter.Convert(percent, typeof(double), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, (double)result, precision: 1);
    }

    [Fact]
    public void Convert_NonDoubleValue_ReturnsZero()
    {
        var result = _converter.Convert("not a double", typeof(double), null!, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }
}

public class ResourceTypeToColorConverterTests
{
    private readonly ResourceTypeToColorConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData("script", "#FBBF24")]
    [InlineData("Script", "#FBBF24")]
    [InlineData("SCRIPT", "#FBBF24")]
    [InlineData("stylesheet", "#A78BFA")]
    [InlineData("image", "#60A5FA")]
    [InlineData("xhr", "#22D3EE")]
    [InlineData("fetch", "#22D3EE")]
    [InlineData("document", "#34D399")]
    [InlineData("other", "#738099")]
    [InlineData("unknown", "#738099")]
    [InlineData("", "#738099")]
    public void Convert_ReturnsCorrectBrush(string resourceType, string expectedHex)
    {
        var result = _converter.Convert(resourceType, typeof(object), null!, _culture);
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        var expected = (Color)ColorConverter.ConvertFromString(expectedHex);
        Assert.Equal(expected, brush.Color);
    }

    [Fact]
    public void Convert_NullValue_ReturnsTertiaryColor()
    {
        var result = _converter.Convert(null!, typeof(object), null!, _culture);
        Assert.IsType<SolidColorBrush>(result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(null!, typeof(string), null!, _culture));
    }
}

public class StatusCodeToColorConverterTests
{
    private readonly StatusCodeToColorConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(null, "#F87171")]
    [InlineData(200, "#34D399")]
    [InlineData(100, "#34D399")]
    [InlineData(299, "#34D399")]
    [InlineData(301, "#FBBF24")]
    [InlineData(399, "#FBBF24")]
    [InlineData(404, "#FB923C")]
    [InlineData(499, "#FB923C")]
    [InlineData(500, "#F87171")]
    [InlineData(503, "#F87171")]
    public void Convert_ReturnsCorrectBrush(int? statusCode, string expectedHex)
    {
        var result = _converter.Convert(statusCode, typeof(object), null!, _culture);
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        var expected = (Color)ColorConverter.ConvertFromString(expectedHex);
        Assert.Equal(expected, brush.Color);
    }

    [Fact]
    public void Convert_StringValue_ReturnsFallbackBrush()
    {
        var result = _converter.Convert("not-a-code", typeof(object), null!, _culture);
        Assert.IsType<SolidColorBrush>(result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(null!, typeof(int), null!, _culture));
    }
}
