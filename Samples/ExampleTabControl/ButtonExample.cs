using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;

namespace ExampleTabControl;

internal class ButtonExample : IUIBuilder
{
    public VisualElement Build() =>
        new Button()
            .Size(new Size(150, 50))
            .Background(new Color(34, 197, 94))
            .BorderBrush(Color.Black)
            .BorderThickness(4)
            .BorderRadius(12)
            .Text("Click Me")
            .OnTapped(() => Console.WriteLine("Event click"));
}
