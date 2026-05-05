using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Reactivity.Hooks;

namespace ModularHooksApp.Components;

public class ToggleSection : UserControl
{
    public override VisualElement Build()
    {
        var isVisible = UseSignal(true);

        return new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(20))
            .BorderRadius(8)
            .Content(
                new VStack()
                    .Spacing(10)
                    .Children(
                        new Checkbox()
                            .IsChecked(isVisible.Value)
                            .OnChanged(v => isVisible.Value = v),
                        
                        new Label("Toggle me to hide the secret message")
                            .FontSize(14)
                            .Foreground(Color.White),

                        new Label("Secret Message: Hooks are awesome!")
                            .FontSize(16)
                            .Foreground(new Color(255, 200, 100))
                            .IsVisible(isVisible)
                    )
            );
    }
}
