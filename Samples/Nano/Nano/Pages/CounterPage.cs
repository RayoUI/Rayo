using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Nano.Pages;

public class CounterPage : Component
{
    private readonly Signal<int> _counter;
    private readonly Computed<string> _counterText;

    public CounterPage(Signal<int> counter, Computed<string> counterText)
    {
        _counter = counter;
        _counterText = counterText;
    }

    public override VisualElement Build()
    {
        return new ScrollView()
            .Content(
                new VStack()
                    .Spacing(18)
                    .Padding(new Thickness(20))
                    .Children(
                        new Frame()
                            .Background(Color.White)
                            .BorderRadius(14)
                            .Padding(new Thickness(20))
                            .Content(
                                new VStack()
                                    .Spacing(8)
                                    .Children(
                                        new Label("Counter")
                                            .FontSize(26)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("A small shared-state page for testing route changes and reactive updates.")
                                            .FontSize(14)
                                            .LineHeight(1.25f)
                                            .Foreground(new Color(91, 103, 122))
                                    )),
                        BuildCounterCard()
                    ));
    }

    private VisualElement BuildCounterCard()
    {
        return new Frame()
            .Background(Color.White)
            .BorderRadius(14)
            .Padding(new Thickness(20))
            .Content(
                new VStack()
                    .Spacing(16)
                    .Children(
                        new Label("Current value")
                            .FontSize(18)
                            .Foreground(new Color(25, 39, 62)),
                        new Label()
                            .Text(_counterText)
                            .FontSize(52)
                            .Foreground(new Color(62, 126, 214))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        new HStack()
                            .Height(48)
                            .Spacing(10)
                            .Alignment(Alignment.Center)
                            .JustifyContent(JustifyContent.Center)
                            .Children(
                                new Button()
                                    .Text("-")
                                    .Size(new Size(56, 48))
                                    .OnTapped(() => _counter.Value--),
                                new Button()
                                    .Text("Reset")
                                    .Size(new Size(96, 48))
                                    .OnTapped(() => _counter.Value = 0),
                                new Button()
                                    .Text("+")
                                    .Size(new Size(56, 48))
                                    .OnTapped(() => _counter.Value++)
                            )
                    ));
    }
}
