using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Extensions;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

namespace FluentExamples;

public class TestExamplesApp : UserControl
{
    private bool active = false;
    public override VisualElement Build()
    {
        return new VStack()
            .Background(Color.Red)
            .Spacing(0)
            .Children(
                new SearchBar()
                    .Ref(out var searchBar)
                    .Margin(10)
                    .Height(40)
                    .Background(Color.Gray)
                    .BorderRadius(5)
                    .OnTextChanged((text) =>
                    {
                        Console.WriteLine("Text changed: " + text);
                    }).OnSearchSubmitted((text) =>
                    {
                        Console.WriteLine("Search submitted: " + text);
                    }),
                new Button()
                    .Text("Change color")
                    .OnTapped(() =>
                    {
                        searchBar.Background = active ? Color.Gray : Color.Green;
                        active = !active;
                    })
            );
    }
}
