using Rayo;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Controls;

namespace Examples;

internal class TabControlExample : IUIBuilder
{
    public VisualElement Build() =>
        new VStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                new TabControl()
                    .Margin(10)
                    .AddTab("Tab 1", new Label().Text("Content of Tab 1").Padding(10))
                    .AddTab("Tab 2", new Label().Text("Content of Tab 2").Padding(10))
                    .AddTab("Tab 3", new Label().Text("Content of Tab 3").Padding(10))
                    .AddTab("Tab 4", new Label().Text("Content of Tab 4").Padding(10))
                    .AddTab("Tab 5", new Label().Text("Content of Tab 5").Padding(10))
                    .AddTab("Tab 6", new Label().Text("Content of Tab 6").Padding(10))
                    .AddTab("Tab 7", new Label().Text("Content of Tab 7").Padding(10))
                    .AddTab("Tab 8", new Label().Text("Content of Tab 8").Padding(10))
                    .AddTab("Tab 9", new Label().Text("Content of Tab 9").Padding(10))
                    .AddTab("Tab 10", new Label().Text("Content of Tab 10").Padding(10))
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
            );
}
