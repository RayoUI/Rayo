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
            .Height(28)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new HStack()
                    .Spacing(10)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label("Console")
                            .Padding(new Thickness(left: 5))
                            .FontSize(14)
                            .Foreground(Color.White),

                        new Frame()
                            .HorizontalAlignment(HorizontalAlignment.Stretch),

                        CreateConsoleIconButton(Icons.Broom)
                            .OnTapped(() => _state.Logs.Value = new List<LogMessage>())
                            .WithTooltip("Clear console"),

                        CreateConsoleIconButton(Icons.ArrowDown)
                            .OnTapped(() => _state.IsConsoleMaximized.Value = !_state.IsConsoleMaximized.Value)
                            .WithTooltip("Minimize console")
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

    private static ButtonIcon CreateConsoleIconButton(IconData icon)
    {
        return new ButtonIcon(icon)
        {
            Width = 24,
            Height = 20,
            IconSize = 12,
            Padding = new Thickness(4),
            Background = new Color(60, 60, 65),
            HoverBackground = new Color(74, 74, 82),
            PressedBackground = new Color(48, 48, 54),
            BorderColor = new Color(75, 75, 82),
            BorderWidth = 1,
            BorderRadius = new CornerRadius(3),
            IconColor = new Color(230, 230, 235)
        };
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
