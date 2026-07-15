using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Reactivity;
using static Rayo.Core.UIHelpers;

namespace Rayo.DevTool.Frames;

public class StatusBarFrame : Component
{
    private readonly DevToolState _state;

    public StatusBarFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        return new Frame()
            .Background(_state.IsConsoleMaximized.Map(maximized =>
                maximized ? DevToolTheme.Colors.SurfaceHover : DevToolTheme.Colors.Surface))
            .Padding(new Thickness(10, 5))
            .Height(30)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label()
                            .Text(_state.ConnectionStatus)
                            .FontSize(11)
                            .Foreground(_state.IsConnected.Map(c =>
                                c ? DevToolTheme.Colors.Success : DevToolTheme.Colors.OnDisabled)),

                        new Frame().HorizontalAlignment(HorizontalAlignment.Stretch), // Flexible space

                        CreateStatusIconButton(DevToolIcons.ArrowUp)
                            .IsVisible(_state.IsConsoleMaximized.Map(m => !m))
                            .OnTapped(() => _state.IsConsoleMaximized.Value = true)
                            .WithTooltip("Show console")
                    )
            );
    }

    private static ButtonIcon CreateStatusIconButton(IconData icon)
    {
        return new ButtonIcon(icon)
        {
            Width = 26,
            Height = 22,
            IconSize = 13,
            Padding = new Thickness(5),
            BorderThickness = 1,
            BorderRadius = new CornerRadius(3),
            Variant = ButtonVariant.Ghost
        };
    }
}
