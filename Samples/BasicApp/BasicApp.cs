using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Styling;

namespace Basic;

public class BasicApp : UserControl
{
    private readonly Signal<int> _counter = new(0);
    private Computed<string> _title;
    private Computed<string> _counterText;

    protected override void OnBeforeBuild()
    {
        _counterText = new Computed<string>(() => _counter.Value.ToString());

        _title = new Computed<string>(() =>
        {
            var title = _counter.Value switch
            {
                > 0 => "😀",
                < 0 => "😞",
                _ => "🙂​"
            };

            return title;
        });
    }

    protected override StyleSheet BuildStyles() =>
    [
        new Style<VStack>("#mainLayout")
            .Background(Color.Azure),
        new Style<Button>(".buttons")
            .Background(Color.Blue),
        new Style<Label>("#title")
            .Foreground(Color.Black)
            .FontSize(30)
            .FontWeight(FontWeight.Bold),
        new Style<Label>("#counter")
            .FontSize(18)
            .Foreground(Color.Black)
            .FontWeight(FontWeight.Bold),
        new Style<Label>("#counter.negative")
            .Foreground(Color.Red),
        new Style<Label>("#counter.positive")
            .Foreground(Color.Blue)
    ];

    public override VisualElement Build()
    {
        return new VStack()
                .Id("mainLayout")
                .JustifyContent(JustifyContent.Center)
                .Children(
                    new Label()
                        .Text(_title)
                        .FontSize(40)
                        .TextHorizontalAlignment(HorizontalAlignment.Center),
                    new Label()
                        .Id("title")
                        .Text("COUNTER")
                        .TextHorizontalAlignment(HorizontalAlignment.Center),
                    new Label()
                        .Id("counter")
                        .Text(_counterText)
                        .TextHorizontalAlignment(HorizontalAlignment.Center)
                        .Bind(_counter, (l, v) =>
                        {
                            l.Classes = v switch
                            {
                                > 0 => "positive",
                                < 0 => "negative",
                                _ => ""
                            };
                        }),
                    new HStack()
                        .Spacing(10)
                        .Padding(new Thickness(20))
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Alignment(Alignment.Center)
                        .JustifyContent(JustifyContent.Center)
                        .Children(
                            new Button()
                                .Classes("buttons")
                                .Text("Decrease")
                                .OnTapped(() => _counter.Value--),
                            new Button()
                                .Classes("buttons")
                                .Text("Reset")
                                .OnTapped(() => _counter.Value = 0),
                            new Button()
                                .Classes("buttons")
                                .Text("Increase")
                                .OnTapped(() => _counter.Value++)
                        )
                );
    }
}
