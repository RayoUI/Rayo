using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class GridPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Grid", "Flexible 2D layout with rows and columns"),

                Helper.CreateExampleSection("Basic 3x3 Grid",
                    new Frame()
                        .Height(200)
                        .Background(new Color(30, 30, 35))
                        .Content(
                            new Grid()
                                .Rows(GridLength.Star, GridLength.Star, GridLength.Star)
                                .Columns(GridLength.Star, GridLength.Star, GridLength.Star)
                                .RowSpacing(10)
                                .ColumnSpacing(10)
                                .Padding(new Thickness(10))
                                .AddChild(CreateCell("0,0", new Color(59, 130, 246)), 0, 0)
                                .AddChild(CreateCell("0,1", new Color(34, 197, 94)), 0, 1)
                                .AddChild(CreateCell("0,2", new Color(234, 179, 8)), 0, 2)
                                .AddChild(CreateCell("1,0", new Color(239, 68, 68)), 1, 0)
                                .AddChild(CreateCell("1,1", new Color(168, 85, 247)), 1, 1)
                                .AddChild(CreateCell("1,2", new Color(236, 72, 153)), 1, 2)
                                .AddChild(CreateCell("2,0", new Color(14, 165, 233)), 2, 0)
                                .AddChild(CreateCell("2,1", new Color(249, 115, 22)), 2, 1)
                                .AddChild(CreateCell("2,2", new Color(20, 184, 166)), 2, 2)
                        )
                ),

                Helper.CreateExampleSection("Spanning Rows and Columns",
                    new Frame()
                        .Height(250)
                        .Background(new Color(30, 30, 35))
                        .Content(
                            new Grid()
                                .Rows(GridLength.Star, GridLength.Star, GridLength.Star)
                                .Columns(GridLength.Star, GridLength.Star, GridLength.Star)
                                .RowSpacing(8)
                                .ColumnSpacing(8)
                                .Padding(new Thickness(8))
                                // Spanning 2 columns
                                .AddChild(CreateCell("ColSpan: 2", new Color(59, 130, 246)), 0, 0, 1, 2)
                                .AddChild(CreateCell("0,2", new Color(34, 197, 94)), 0, 2)
                                // Spanning 2 rows
                                .AddChild(CreateCell("RowSpan: 2", new Color(234, 179, 8)), 1, 0, 2, 1)
                                .AddChild(CreateCell("1,1", new Color(239, 68, 68)), 1, 1)
                                .AddChild(CreateCell("1,2", new Color(168, 85, 247)), 1, 2)
                                .AddChild(CreateCell("2,1", new Color(236, 72, 153)), 2, 1)
                                .AddChild(CreateCell("2,2", new Color(14, 165, 233)), 2, 2)
                        )
                ),

                Helper.CreateExampleSection("Mixed Sizes (Pixel, Auto, Star)",
                    new Frame()
                        .Height(200)
                        .Background(new Color(30, 30, 35))
                        .Content(
                            new Grid()
                                .Rows(GridLength.Pixels(50), GridLength.Star)
                                .Columns(GridLength.Auto, GridLength.Star)
                                .RowSpacing(10)
                                .ColumnSpacing(10)
                                .Padding(new Thickness(10))
                                .AddChild(CreateCell("Header (50px high)", new Color(75, 85, 99)), 0, 0, 1, 2)
                                .AddChild(new Label("Sidebar (Auto Width)").Foreground(Color.White).Padding(new Thickness(10)), 1, 0)
                                .AddChild(CreateCell("Content (Remaining Space)", new Color(31, 41, 55)), 1, 1)
                        )
                )
            );
    }

    private VisualElement CreateCell(string text, Color color)
    {
        return new Frame()
            .Background(color)
            .BorderRadius(4)
            .Content(
                new Label(text)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(Color.White)
                    .Padding(new Thickness(4))
                    .FontSize(12)
            );
    }
}
