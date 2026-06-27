using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using System.Collections.Generic;

namespace Rayo.DevTool.Frames;


public class ConsoleFrame : Component
{
    private readonly DevToolState _state;

    public ConsoleFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        var header = new Frame()
            .Background(DevToolTheme.Colors.SurfaceHover)
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
                            .Foreground(DevToolTheme.Colors.OnSurface),

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
            .Background(DevToolTheme.Colors.Background)
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
            BorderThickness = 1,
            BorderRadius = new CornerRadius(3),
            Variant = ButtonVariant.Ghost
        };
    }

    private VisualElement BuildLogList()
    {
        var container = new VStack()
            .Spacing(1)
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top);

        void UpdateLogs(List<LogMessage> logs)
        {
            container.ClearChildren();
            foreach (var log in logs)
            {
                var color = log.Level switch
                {
                    "Error" => DevToolTheme.Colors.Danger,
                    "Warning" => DevToolTheme.Colors.Warning,
                    "Info" => DevToolTheme.Colors.Info,
                    "Trace" => DevToolTheme.Colors.OnDisabled,
                    _ => DevToolTheme.Colors.OnSurface
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
                                        .Foreground(DevToolTheme.Colors.OnSurface)
                                        .FontSize(11)
                                )
                        )
                );
            }
        }

        _state.Logs.Subscribe(UpdateLogs);
        UpdateLogs(_state.Logs.Value);

        return container;
    }
}
