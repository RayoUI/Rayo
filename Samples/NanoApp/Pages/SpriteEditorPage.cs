using NanoApp.Controls;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace NanoApp.Pages;

public sealed class SpriteEditorPage : Component
{
    private readonly SpriteDocument _document;
    private readonly SpriteCanvas _canvas;

    public SpriteEditorPage(SpriteDocument document)
    {
        _document = document;
        _canvas = new SpriteCanvas(document);
    }

    public override VisualElement Build()
    {
        var layout = new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Pixels(210), GridLength.Star)
            .Background(new Color(12, 16, 24));

        layout
            .AddChild(BuildToolsPanel(), 0, 0)
            .AddChild(_canvas, 0, 1);

        return layout;
    }

    private VisualElement BuildToolsPanel()
    {
        return new Frame()
            .Background(new Color(20, 27, 40))
            .BorderBrush(new Color(45, 55, 72))
            .BorderThickness(new Thickness(0, 0, 1, 0))
            .Padding(new Thickness(14))
            .Content(
                new VStack()
                    .Spacing(12)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Children(
                        new Label("Sprite Editor")
                            .FontSize(16)
                            .FontWeight(FontWeight.Bold)
                            .Foreground(Color.White),
                        new Label("16 × 16 pixels")
                            .FontSize(12)
                            .Foreground(new Color(148, 163, 184)),
                        BuildPalette(),
                        new Button()
                            .Text("Eraser")
                            .Height(36)
                            .OnTapped(() => _document.SelectedColor = null),
                        new Button()
                            .Text("Clear sprite")
                            .Height(36)
                            .Background(new Color(51, 65, 85))
                            .HoverBackground(new Color(71, 85, 105))
                            .OnTapped(_canvas.Clear)));
    }

    private VisualElement BuildPalette()
    {
        var colors = new[]
        {
            new Color(15, 23, 42),
            Color.White,
            new Color(239, 68, 68),
            new Color(249, 115, 22),
            new Color(250, 204, 21),
            new Color(34, 197, 94),
            new Color(6, 182, 212),
            new Color(14, 165, 233),
            new Color(99, 102, 241),
            new Color(168, 85, 247),
            new Color(236, 72, 153),
            new Color(148, 163, 184)
        };

        var palette = new Grid()
            .Rows(
                GridLength.Pixels(38),
                GridLength.Pixels(38),
                GridLength.Pixels(38))
            .Columns(
                GridLength.Star,
                GridLength.Star,
                GridLength.Star,
                GridLength.Star)
            .RowSpacing(6)
            .ColumnSpacing(6);

        for (var index = 0; index < colors.Length; index++)
        {
            var color = colors[index];
            palette.AddChild(
                new Button()
                    .Text("")
                    .Background(color)
                    .HoverBackground(new Color(
                        MathF.Min(1, color.R + 0.12f),
                        MathF.Min(1, color.G + 0.12f),
                        MathF.Min(1, color.B + 0.12f)))
                    .BorderBrush(new Color(203, 213, 225))
                    .BorderThickness(1)
                    .BorderRadius(5)
                    .OnTapped(() => _document.SelectedColor = color),
                index / 4,
                index % 4);
        }

        return palette;
    }
}
