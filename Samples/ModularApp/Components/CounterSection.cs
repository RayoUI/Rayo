using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Reactivity.Hooks;

namespace ModularHooksApp.Components;

public class CounterSection : UserControl
{
    public override VisualElement Build()
    {
        var count = UseSignal(0);

        return new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(20))
            .BorderRadius(8)
            .Content(
                new VStack()
                    .Spacing(10)
                    .Children(
                        new Label("Counter (State Persists)")
                            .FontSize(18)
                            .Foreground(Color.White),
                        
                        new HStack()
                            .Spacing(15)
                            .Children(
                                new Button()
                                    .Text("-")
                                    .Width(40)
                                    .Background(new Color(200, 60, 60))
                                    .OnTapped(() => count.Value--),
                                
                                new Label()
                                    .FontSize(24)
                                    .Foreground(Color.White)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                                    .Text(count.Map(c => c.ToString())),
                                    
                                new Button()
                                    .Text("+")
                                    .Width(40)
                                    .Background(new Color(60, 200, 60))
                                    .OnTapped(() => count.Value++)
                            )
                    )
            );
    }
}
