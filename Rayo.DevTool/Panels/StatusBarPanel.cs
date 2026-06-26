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
                maximized ? new Color(30, 30, 35) : new Color(36, 42, 52)))
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
                            .Foreground(_state.IsConnected.Map(c => c ? ColorDefault.Success : ColorDefault.Secondary)),

                        new Frame().HorizontalAlignment(HorizontalAlignment.Stretch), // Flexible space

                        CreateStatusIconButton(Icons.ArrowUp)
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
            Background = new Color(48, 58, 72),
            HoverBackground = new Color(62, 74, 92),
            PressedBackground = new Color(42, 50, 64),
            BorderBrush = new Color(74, 88, 110),
            BorderThickness = 1,
            BorderRadius = new CornerRadius(3),
            IconColor = new Color(225, 232, 242)
        };
    }
}
