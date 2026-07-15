using Rayo.Controls;
using Rayo.Layout;

namespace Rayo.Tests;

public sealed class HStackLayoutTests
{
    [Fact]
    public void Explicitly_sized_children_keep_configured_spacing()
    {
        var first = new Frame { Width = 36, Height = 36 };
        var second = new Frame { Width = 36, Height = 36 };
        var third = new Frame { Width = 36, Height = 36 };
        var layout = new HStack()
            .Spacing(6)
            .Children(first, second, third);

        layout.MeasureUpdate(390, 56);
        layout.ArrangeUpdate(0, 0, layout.DesiredWidth, 56);

        Assert.Equal(0, first.ComputedX);
        Assert.Equal(42, second.ComputedX);
        Assert.Equal(84, third.ComputedX);
    }

    [Fact]
    public void Explicit_icon_button_size_is_not_expanded_over_spacing()
    {
        var first = new ButtonIcon { Width = 36, Height = 36 };
        var second = new ButtonIcon { Width = 36, Height = 36 };
        var layout = new HStack()
            .Spacing(10)
            .Children(first, second);

        layout.MeasureUpdate(390, 56);
        layout.ArrangeUpdate(0, 0, layout.DesiredWidth, 56);

        Assert.Equal(36, first.ComputedWidth);
        Assert.Equal(46, second.ComputedX);
        Assert.Equal(10, second.ComputedX - (first.ComputedX + first.ComputedWidth));
    }
}
