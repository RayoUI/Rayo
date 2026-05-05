using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Rayo.DevTool.Frames;

public class ToolbarFrame : UserControl
{
    private readonly DevToolState _state;

    public ToolbarFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        return new Frame()
            .Background(new Color(35, 35, 40))
            .Padding(new Thickness(10, 8))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .Spacing(10)
                    .Alignment(Alignment.Center)
                    .Height(30)
                    .Children(
                        new Label("Host:")
                            .Foreground(ColorDefault.Secondary),

                        new Entry()
                            .Text(_state.Host.Value)
                            .Width(120)
                            .Placeholder("localhost")
                            .OnTextChanged(text => _state.Host.Value = text),

                        new Label("Port:")
                            .Foreground(ColorDefault.Secondary),

                        new Entry()
                            .Text(_state.Port.Value.ToString())
                            .Width(60)
                            .OnTextChanged(text => {
                                if (int.TryParse(text, out var p)) _state.Port.Value = p;
                            }),

                        new Button()
                            .Text(_state.IsConnected.Map(c => c ? "Disconnect" : "Connect"))
                            .Background(_state.IsConnected.Map(c => c ? ColorDefault.Danger : ColorDefault.Success))
                            .Width(100)
                            .OnTapped(async () =>
                            {
                                if (_state.Client.IsConnected)
                                {
                                    await _state.DisconnectManuallyAsync();
                                }
                                else
                                {
                                    await _state.ConnectManuallyAsync();
                                }
                            }),

                        new Button()
                            .Text("Refresh")
                            .Background(ColorDefault.Primary)
                            .Width(80)
                            .OnTapped(async () => await _state.RefreshTreeAsync()),

                        new Frame().Width(12) // Spacer

                        //new Label()
                        //    .Text(_state.ConnectionStatus)
                        //    .Foreground(_state.IsConnected.Map(c => c ? ColorDefault.Success : ColorDefault.Secondary))
                    )
            );
    }
}
