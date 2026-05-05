using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;

namespace ExampleTabControl;

internal class PanelExample : IUIBuilder
{
    public VisualElement Build() =>
        new Frame()
            .Size(new Size(300, 300))
            .Background(Color.Red)
            .Content(
                new Frame()
                    .Margin(new Thickness(100, 10, 10, 10))
                    .Background(new Color(147, 197, 253))
                    .Size(new Size(200, 200))
                    .BorderColor(Color.Black)
                    .BorderWidth(8)
                    .BorderRadius(16)
            );
}
