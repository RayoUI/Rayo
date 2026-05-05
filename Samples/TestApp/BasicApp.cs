using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Styling;

namespace TestApp;

public class BasicApp : UserControl
{
    private readonly Signal<int> _counter = new(0);

    protected override void OnBeforeBuild()
    {

    }

    protected override StyleSheet BuildStyles() =>
    [

    ];

    public override VisualElement Build()
    {
        return new VStack()
                .Id("mainLayout")
                .JustifyContent(JustifyContent.Center)
                .Children(
                    new Label()
                        .Ref(out var label)
                        .Text("Hello World")
                        .FontSize(40)
                        .TextHorizontalAlignment(HorizontalAlignment.Center),
                   new Button()
                        .Text("Click me")
                        .Width(120)
                        .Height(40)
                        .OnTapped(() => label.Background = label.Background == Color.Transparent ? Color.Blue : Color.Transparent)
                );
    }
}
