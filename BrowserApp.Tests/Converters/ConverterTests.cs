using System.Globalization;
using System.Windows;
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
