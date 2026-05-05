using PaintApp.Controls;
using PaintApp.ViewModels;
using PaintApp.Views;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using SkiaSharp;

namespace PaintApp;

/// <summary>
/// Root UserControl — creates the ViewModel and canvas, wires position feedback,
/// and composes the four main sub-views into the window layout.
/// </summary>
public class PaintAppUI : UserControl
{
    private static PaintViewState vm = new();
    public override VisualElement Build()
    {
        var canvas = MakePaintCanvas();
        return new VStack()
            .Spacing(0)
            .Children(
                new ToolbarView(vm, canvas),
                MakeScrollView(canvas),
                new ColorPaletteView(vm),
                new StatusBarView(vm)
            );
    }

    private PaintCanvas MakePaintCanvas()
    {
        return new PaintCanvas()
            .Ref(out var canvas)
            .OnPositionChangedHandler(pos =>
                vm.PositionText.Value = pos.X < 0
                ? ""
                : $"{(int)(pos.X - canvas.ComputedX)}, {(int)(pos.Y - canvas.ComputedY)}"
            );
    }

    private static VisualElement MakeScrollView(PaintCanvas canvas)
    {
        return new ScrollView()
            .ScrollbarBackground(new Color(110, 110, 110))
            .ScrollbarThumb(new Color(70, 70, 70))
            .Orientation(ScrollOrientation.Both)
            .Background(new Color(150, 150, 150))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(canvas);
                
    }
}
