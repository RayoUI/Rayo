using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Reactivity.Hooks;

namespace ModularHooksApp.Components;

public class InputSection : UserControl
{
    public override VisualElement Build()
    {
        var name = UseSignal("Developer");

        return new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(20))
            .BorderRadius(8)
            .Content(
                new VStack()
                    .Spacing(10)
                    .Children(
                        new Label("Text Input (State Persists)")
                            .FontSize(18)
                            .Foreground(Color.White),

                        new Entry()
                            .Placeholder("Enter your name...")
                            .Text(name.Value)
                            .OnTextChanged(text => name.Value = text),

                        new Label()
                            .FontSize(16)
                            .Foreground(new Color(100, 255, 100))
                            .Text(name.Map(n => $"Hello, {n}!"))
                    )
            );
    }
}
