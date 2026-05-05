using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;

namespace ExampleTabControl;

internal class HStackExample : IUIBuilder
{
    public VisualElement Build() =>
        new ScrollView
        {
            ShowHorizontalScrollbar = true
        }.Content(
            new HStack()
                .Padding(new Thickness(10))
                .Spacing(5)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Background(Color.Blue)
                .Children(
                    CreateTile(new Color(239, 68, 68)),
                    CreateTile(new Color(34, 197, 94)),
                    CreateTile(new Color(59, 130, 246)),
                    CreateTile(new Color(239, 68, 68)),
                    CreateTile(new Color(34, 197, 94)),
                    CreateTile(new Color(59, 130, 246)),
                    CreateTile(new Color(239, 68, 68)),
                    CreateTile(new Color(34, 197, 94)),
                    CreateTile(new Color(59, 130, 246)),
                    CreateTile(new Color(239, 68, 68)),
                    CreateTile(new Color(34, 197, 94)),
                    CreateTile(new Color(59, 130, 246)),
                    CreateTile(new Color(239, 68, 68)),
                    CreateTile(new Color(34, 197, 94)),
                    CreateTile(new Color(59, 130, 246)),
                    CreateTile(new Color(239, 68, 68)),
                    CreateTile(new Color(34, 197, 94)),
                    CreateTile(new Color(59, 130, 246))
                )
        );

    private static VisualElement CreateTile(Color color) =>
        new Frame()
            .Background(color)
            .Size(new Size(100, 100))
            .BorderRadius(8);
}
