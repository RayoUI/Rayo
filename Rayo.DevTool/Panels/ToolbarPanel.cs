using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Rayo.DevTool.Frames;

public class ToolbarFrame : Component
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

                        new Frame()
                            .HorizontalAlignment(HorizontalAlignment.Stretch),

                        BuildSettingsButton()

                        //new Label()
                        //    .Text(_state.ConnectionStatus)
                        //    .Foreground(_state.IsConnected.Map(c => c ? ColorDefault.Success : ColorDefault.Secondary))
                    )
            );
    }

    private VisualElement BuildSettingsButton()
    {
        var button = new ButtonIcon(Icons.Settings)
        {
            Width = 30,
            Height = 30,
            IconSize = 15,
            Padding = new Thickness(6),
            Background = new Color(40, 40, 46),
            HoverBackground = new Color(55, 55, 62),
            PressedBackground = new Color(32, 32, 38),
            BorderBrush = new Color(60, 60, 68),
            BorderThickness = 1,
            BorderRadius = new CornerRadius(3),
            IconColor = new Color(220, 220, 230)
        };

        button.Tapped += _ => ShowSettingsDialog();
        return button.WithTooltip("Open DevTools settings");
    }

    private void ShowSettingsDialog()
    {
        VisualElement? overlay = null;

        var showComputed = new Checkbox("Show computed properties")
        {
            IsChecked = _state.ShowComputedProperties.Value,
            FontSize = 13,
            BoxSize = 16,
            LabelColor = new Color(220, 220, 230),
            Background = new Color(38, 38, 44),
            HoverBackground = new Color(48, 48, 56),
            CheckedBackground = new Color(34, 197, 94)
        };

        showComputed.Changed += value => _state.ShowComputedProperties.Value = value;

        var content = new VStack()
            .Spacing(12)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(showComputed);

        overlay = new Dialog("DevTools Settings", content, () =>
        {
            if (overlay != null)
                OverlayManager.RemoveOverlay(overlay);
        }).Build();

        OverlayManager.AddOverlay(overlay);
    }
}
