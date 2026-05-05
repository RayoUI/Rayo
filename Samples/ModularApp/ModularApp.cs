using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using ModularHooksApp.Components;
using Rayo;

namespace ModularHooksApp;

public class ModularApp : UserControl
{
    public override VisualElement Build()
    {
        return new ScrollView()
            .Content(
                new VStack()
                    .Spacing(20)
                    .Padding(new Thickness(30))
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Background(new Color(30, 30, 35))
                    .Children(
                        new Label("🐸 Modular Hooks App")
                            .FontSize(28)
                            .Foreground(new Color(100, 200, 255))
                            .HorizontalAlignment(HorizontalAlignment.Center),

                        new Label("This app demonstrates a modular architecture using Hooks.")
                            .FontSize(14)
                            .Foreground(Color.Gray)
                            .HorizontalAlignment(HorizontalAlignment.Center),

                        // Modular Components
                        new CounterSection(),
                        new InputSection(),
                        new ToggleSection(),
                        
                        // Use the new Component pattern
                        new SharedStateSection(),

                        new Button()
                            .Text("Reset All State")
                            .Background(Color.Gray)
                            .Margin(new Thickness(20))
                            .OnTapped(() => {
                                Hooks.ResetAll();
                                Rebuild();
                                Console.WriteLine("State reset and UI rebuilt.");
                            })
                    )
            );
    }
}
