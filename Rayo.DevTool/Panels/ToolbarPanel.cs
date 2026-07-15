using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
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
            .Background(DevToolTheme.Colors.Surface)
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
                            .Foreground(DevToolTheme.Colors.OnDisabled),

                        new Entry()
                            .Text(_state.Host.Value)
                            .Width(120)
                            .Placeholder("localhost")
                            .OnTextChanged(text => _state.Host.Value = text),

                        new Label("Port:")
                            .Foreground(DevToolTheme.Colors.OnDisabled),

                        new Entry()
                            .Text(_state.Port.Value.ToString())
                            .Width(60)
                            .OnTextChanged(text => {
                                if (int.TryParse(text, out var p)) _state.Port.Value = p;
                            }),

                        new Button()
                            .Text(_state.IsConnected.Map(c => c ? "Disconnect" : "Connect"))
                            .Variant(_state.IsConnected.Map(c => c ? ButtonVariant.Danger : ButtonVariant.Primary))
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
                            .Variant(ButtonVariant.Primary)
                            .Width(80)
                            .OnTapped(async () => await _state.RefreshTreeAsync()),

                        new Frame()
                            .HorizontalAlignment(HorizontalAlignment.Stretch),

                        BuildThemeButton(),
                        BuildSettingsButton()

                        //new Label()
                        //    .Text(_state.ConnectionStatus)
                        //    .Foreground(_state.IsConnected.Map(c => c ? ColorDefault.Success : ColorDefault.Secondary))
                    )
            );
    }

    private VisualElement BuildThemeButton()
    {
        var app = UIApplication.Current;
        var isDark = app?.ActiveTheme.Brightness == ThemeBrightness.Dark;
        var button = new ButtonIcon(isDark ? DevToolIcons.Sun : DevToolIcons.Moon)
        {
            Width = 30,
            Height = 30,
            IconSize = 15,
            Padding = new Thickness(6),
            BorderThickness = 1,
            BorderRadius = new CornerRadius(3),
            Variant = ButtonVariant.Ghost
        };

        button.Tapped += _ =>
        {
            var currentApp = UIApplication.Current;
            if (currentApp == null)
                return;

            currentApp.UseTheme(
                currentApp.ActiveTheme.Brightness == ThemeBrightness.Dark
                    ? RayoThemes.Light
                    : RayoThemes.Dark);
        };

        return button.WithTooltip(isDark
            ? "Switch to light theme"
            : "Switch to dark theme");
    }

    private VisualElement BuildSettingsButton()
    {
        var button = new ButtonIcon(Icons.Settings)
        {
            Width = 30,
            Height = 30,
            IconSize = 15,
            Padding = new Thickness(6),
            BorderThickness = 1,
            BorderRadius = new CornerRadius(3),
            Variant = ButtonVariant.Ghost
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
            BoxSize = 16
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
