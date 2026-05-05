using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Reactivity.Hooks;
using Rayo;

namespace ModularHooksApp.Components;

// 1. Convert to class inheriting from Component
public class SharedStateSection : UserControl
{
    public override VisualElement Build()
    {
        // 1. Lift State Up: Create state in the parent component
        // Since this is an instance method of an IUIBuilder (Component), Hooks work correctly here.
        var sharedValue = UseSignal(50);

        return new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(20))
            .BorderRadius(8)
            .Content(
                new VStack()
                    .Spacing(15)
                    .Children(
                        new Label("🐔 Component Communication (Shared State)")
                            .FontSize(18)
                            .Foreground(Color.White),

                        new Label("Refactored to use Component classes and Props!")
                            .FontSize(12)
                            .Foreground(Color.Gray),

                        new HStack()
                            .Spacing(20)
                            .Children(
                                // 2. Instantiate child components passing props via constructor
                                new ControlComponent(sharedValue),
                                new DisplayComponent(sharedValue)
                            )
                    )
            );
    }
}

// 2. Child component receiving Props
public class ControlComponent(IWritableSignal<int> value) : UserControl
{

    public override VisualElement Build()
    {
        return new Frame()
            .Background(new Color(50, 50, 60))
            .Padding(new Thickness(10))
            .BorderRadius(4)
            .Content(
                new VStack()
                    .Spacing(5)
                    .Children(
                        new Label("Controller (Child 1)").FontSize(12).Foreground(Color.LightGray),
                        new HStack()
                            .Spacing(5)
                            .Children(
                                new Button()
                                    .Text("-10")
                                    .Width(50)
                                    .Background(new Color(200, 60, 60))
                                    .OnTapped(() => value.Value -= 10),
                                new Button()
                                    .Text("+10")
                                    .Width(50)
                                    .Background(new Color(60, 200, 60))
                                    .OnTapped(() => value.Value += 10)
                            )
                    )
            );
    }
}

// 3. Another child component receiving Props
public class DisplayComponent(IReadableSignal<int> value) : UserControl
{
    public override VisualElement Build()
    {
        return new Frame()
            .Background(new Color(50, 50, 60))
            .Padding(new Thickness(10))
            .BorderRadius(4)
            .Content(
                new VStack()
                    .Spacing(5)
                    .Children(
                        new Label("Display (Child 2)").FontSize(12).Foreground(Color.LightGray),
                        new Label()
                            .FontSize(20)
                            .Foreground(Color.White)
                            .Text(value.Map(v => $"Value: {v}"))
                    )
            );
    }
}
