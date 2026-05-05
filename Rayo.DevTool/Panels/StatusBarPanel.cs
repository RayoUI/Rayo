using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Reactivity;
using static Rayo.Core.UIHelpers;

namespace Rayo.DevTool.Frames;

public class StatusBarFrame : UserControl
{
    private readonly DevToolState _state;

    public StatusBarFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        return new Frame()
            .Background(new Color(30, 30, 35))
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

                        new Button()
                            .Text("Show Console")
                            .FontSize(10)
                            .Width(100)
                            .Background(ColorDefault.Info)
                            .IsVisible(_state.IsConsoleMaximized.Map(m => !m))
                            .OnTapped(() => _state.IsConsoleMaximized.Value = true)
                    )
            );
    }
}
