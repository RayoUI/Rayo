using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using System.Collections.Generic;

namespace Rayo.DevTool.Frames;


public class ConsoleFrame : UserControl
{
    private readonly DevToolState _state;

    public ConsoleFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        var header = new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(2))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .Spacing(10)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label("Console")
                            .FontSize(14)
                            .Foreground(Color.White),
                        new Button()
                            .Text("Clear")
                            .FontSize(10)
                            .Background(new Color(60, 60, 65))
                            .Width(60)
                            .Height(20)
                            .OnTapped(() => _state.Logs.Value = new List<LogMessage>()),

                        new Button()
                            .Text(_state.IsConsoleMaximized.Map(max => max ? "Minimize" : "Maximize"))
                            .FontSize(10)
                            .Width(80)
                            .Height(20)
                            .Background(new Color(60, 60, 65))
                            .OnTapped(() => _state.IsConsoleMaximized.Value = !_state.IsConsoleMaximized.Value)
                    )
            );

        var logScroll = new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                BuildLogList()
            );

        return new Grid()
            .Background(new Color(20, 20, 25))
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Rows(GridLength.Auto, GridLength.Star)
            .Columns(GridLength.Star)
            .AddChild(header, 0, 0)
            .AddChild(logScroll, 1, 0);
    }

    private VisualElement BuildLogList()
    {
        var container = new VStack()
            .Spacing(1)
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top);

        _state.Logs.Subscribe(logs =>
        {
            container.ClearChildren();
            foreach (var log in logs)
            {
                var color = log.Level switch
                {
                    "Error" => ColorDefault.Danger,
                    "Warning" => ColorDefault.Warning,
                    "Info" => ColorDefault.Secondary,
                    "Trace" => new Color(150, 150, 150),
                    _ => Color.White
                };

                container.AddChild(
                    new Frame()
                        .Padding(new Thickness(4, 2))
                        .Content(
                            new HStack()
                                .Spacing(8)
                                .Children(
                                    new Label($"[{log.Level}]")
                                        .Foreground(color)
                                        .FontSize(10)
                                        .Width(60),
                                    new Label(log.Message)
                                        .Foreground(new Color(220, 220, 220))
                                        .FontSize(11)
                                )
                        )
                );
            }
        });

        return container;
    }
}
