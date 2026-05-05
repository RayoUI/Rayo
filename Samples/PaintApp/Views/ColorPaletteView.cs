using PaintApp.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Rendering;
using Rayo.Core;
using Rayo.Layout;

namespace PaintApp.Views;

/// <summary>Color palette strip — 24 preset swatches that update the active draw color.</summary>
public class ColorPaletteView : UserControl
{
    private readonly PaintViewState _vm;

    private static readonly Color[] PaletteColors =
    [
        Color.Black, Color.White,
        new(128, 128, 128), new(192, 192, 192),
        Color.Red,          new(128, 0, 0),
        Color.Green,        new(0, 128, 0),
        Color.Blue,         new(0, 0, 128),
        Color.Yellow,       new(128, 128, 0),
        Color.Cyan,         new(0, 128, 128),
        Color.Magenta,      new(128, 0, 128),
        Color.Orange,       new(255, 128, 0),
        new(255, 192, 128), new(0, 64, 128),
        new(139, 90, 43),   new(255, 215, 0),
        new(255, 105, 180), new(64, 0, 128),
    ];

    public ColorPaletteView(PaintViewState vm) => _vm = vm;

    public override VisualElement Build()
    {
        var swatches = PaletteColors
            .Select(MakeSwatch)
            .ToArray();

        return new Frame()
            .Background(new Color(245, 245, 245))
            .VerticalAlignment(VerticalAlignment.Bottom)
            .Content(
                new HStack()
                    .Spacing(3)
                    .Padding(new Thickness(8, 4))
                    .Children(swatches)
            );
    }

    private VisualElement MakeSwatch(Color color) =>
        new Button()
            .Text("")
            .Background(color)
            .Width(22)
            .Height(22)
            .BorderRadius(new CornerRadius(2))
            // Setting PrimaryColor triggers ToolbarView's subscription, which syncs the picker.
            .OnTapped(() => _vm.PrimaryColor.Value = color);
}
