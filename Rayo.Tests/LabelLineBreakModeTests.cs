using Rayo.Controls;
using Rayo.Core;

namespace Rayo.Tests;

public sealed class LabelLineBreakModeTests
{
    [Fact]
    public void WordWrap_uses_available_width_to_increase_desired_height()
    {
        var label = new Label("aa aa") { FontSize = 10 };

        label.MeasureUpdate(16, float.PositiveInfinity);

        Assert.True(label.DesiredHeight > 13.5f);
        Assert.True(label.DesiredWidth <= 16);
    }

    [Fact]
    public void NoWrap_retains_single_line_intrinsic_measurement()
    {
        var label = new Label("aa aa")
        {
            FontSize = 10,
            LineBreakMode = LineBreakMode.NoWrap,
        };

        label.MeasureUpdate(16, float.PositiveInfinity);

        Assert.Equal(13.5f, label.DesiredHeight);
        Assert.True(label.DesiredWidth > 16);
    }

    [Fact]
    public void Explicit_line_breaks_are_preserved_when_wrapping()
    {
        var label = new Label("aa\naa") { FontSize = 10 };

        label.MeasureUpdate(100, float.PositiveInfinity);

        Assert.Equal(28.5f, label.DesiredHeight);
    }

    [Fact]
    public void CharacterSpacing_is_included_in_the_desired_width()
    {
        var label = new Label("aa")
        {
            FontSize = 10,
            CharacterSpacing = 3,
            LineBreakMode = LineBreakMode.NoWrap,
        };

        label.MeasureUpdate(float.PositiveInfinity, float.PositiveInfinity);

        Assert.Equal(15, label.DesiredWidth);
    }

    [Fact]
    public void MaxLines_limits_the_desired_height()
    {
        var label = new Label("aa aa aa")
        {
            FontSize = 10,
            MaxLines = 2,
        };

        label.MeasureUpdate(16, float.PositiveInfinity);

        Assert.Equal(28.5f, label.DesiredHeight);
    }
}
