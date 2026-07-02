using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Rayo.Tests;

public sealed class GridLayoutTests
{
    [Fact]
    public void Auto_row_uses_height_measured_at_actual_star_column_width()
    {
        var responsiveChild = new WidthSensitiveElement();
        var grid = new Grid()
            .Rows(GridLength.Auto)
            .Columns(GridLength.Pixels(130), GridLength.Star)
            .AddChild(responsiveChild, 0, 1);

        grid.MeasureUpdate(230, float.PositiveInfinity);

        Assert.Equal(100, responsiveChild.LastAvailableWidth);
        Assert.Equal(40, responsiveChild.DesiredHeight);
        Assert.Equal(40, grid.DesiredHeight);
    }

    [Fact]
    public void Auto_row_measures_stretch_content_at_its_natural_height()
    {
        var child = new HeightSensitiveElement();
        var grid = new Grid()
            .Rows(GridLength.Auto)
            .Columns(GridLength.Star)
            .AddChild(child, 0, 0);

        grid.MeasureUpdate(200, 500);

        Assert.True(float.IsPositiveInfinity(child.LastAvailableHeight));
        Assert.Equal(26, child.DesiredHeight);
        Assert.Equal(26, grid.DesiredHeight);
    }

    private sealed class WidthSensitiveElement : VisualElement
    {
        public float LastAvailableWidth { get; private set; }

        protected override void Measure(float availableWidth, float availableHeight)
        {
            LastAvailableWidth = availableWidth;
            DesiredWidth = availableWidth;
            DesiredHeight = availableWidth <= 100 ? 40 : 20;
        }

        public override void Render(IRenderer renderer)
        {
        }
    }

    private sealed class HeightSensitiveElement : VisualElement
    {
        public float LastAvailableHeight { get; private set; }

        protected override void Measure(float availableWidth, float availableHeight)
        {
            LastAvailableHeight = availableHeight;
            DesiredWidth = availableWidth;
            DesiredHeight = float.IsPositiveInfinity(availableHeight)
                ? 26
                : availableHeight;
        }

        public override void Render(IRenderer renderer)
        {
        }
    }
}
