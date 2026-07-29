using Rayo.Core;
using Rayo.Controls;
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

    [Fact]
    public void Spacing_remains_between_stretched_grid_cells()
    {
        var first = new TestElement
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var second = new TestElement
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var grid = new Grid()
            .Rows(GridLength.Pixels(40), GridLength.Pixels(40))
            .Columns(GridLength.Star, GridLength.Star, GridLength.Star)
            .ColumnSpacing(8)
            .RowSpacing(7)
            .AddChild(first, 0, 0)
            .AddChild(second, 1, 1);

        grid.MeasureUpdate(316, 87);
        grid.ArrangeUpdate(0, 0, 316, 87);

        Assert.Equal(100, first.ComputedWidth);
        Assert.Equal(40, first.ComputedHeight);
        Assert.Equal(108, second.ComputedX);
        Assert.Equal(47, second.ComputedY);
        Assert.Equal(100, second.ComputedWidth);
        Assert.Equal(40, second.ComputedHeight);
    }

    [Fact]
    public void Spanned_cell_includes_only_internal_spacing()
    {
        var child = new TestElement
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var grid = new Grid()
            .Rows(GridLength.Pixels(40))
            .Columns(GridLength.Star, GridLength.Star, GridLength.Star)
            .ColumnSpacing(8)
            .AddChild(child, 0, 0, columnSpan: 2);

        grid.MeasureUpdate(316, 40);
        grid.ArrangeUpdate(0, 0, 316, 40);

        Assert.Equal(208, child.ComputedWidth);
    }

    [Fact]
    public void Auto_column_does_not_consume_a_spanning_header_width()
    {
        var header = new Frame();
        var sidebar = new Label("Sidebar") { Width = 110 };
        var content = new Frame();
        var grid = new Grid()
            .Rows(GridLength.Pixels(50), GridLength.Star)
            .Columns(GridLength.Auto, GridLength.Star)
            .ColumnSpacing(10)
            .AddChild(header, 0, 0, rowSpan: 1, columnSpan: 2)
            .AddChild(sidebar, 1, 0)
            .AddChild(content, 1, 1);

        grid.MeasureUpdate(300, 200);
        grid.ArrangeUpdate(0, 0, 300, 200);

        Assert.Equal(110, sidebar.ComputedWidth);
        Assert.True(content.ComputedWidth > 0);
        Assert.True(content.ComputedX + content.ComputedWidth <= 300);
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

    private sealed class TestElement : VisualElement
    {
        protected override void Measure(float availableWidth, float availableHeight)
        {
            DesiredWidth = availableWidth;
            DesiredHeight = availableHeight;
        }

        public override void Render(IRenderer renderer)
        {
        }
    }
}
