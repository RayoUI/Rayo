using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Layout;
using Rayo.Rendering;

namespace Rayo.DevTool.Frames;

/// <summary>Live token and semantic-color inspector for the connected application.</summary>
public sealed class ThemeInspectorFrame : Component
{
    private readonly DevToolState _state;

    public ThemeInspectorFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        var content = new VStack()
            .Spacing(10)
            .Padding(new Thickness(10))
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        void Update(ThemeSnapshotResponse? snapshot)
        {
            content.ClearChildren();
            if (snapshot == null)
            {
                content.AddChild(Muted(
                    _state.IsConnected.Value
                        ? "Loading theme from the connected application..."
                        : "Connect to an application to inspect its theme."));
                content.MarkNeedsLayout();
                return;
            }

            content.AddChild(Heading($"Theme: {snapshot.Name}"));
            content.AddChild(Muted(
                $"{snapshot.Brightness} · {snapshot.Density} · text {snapshot.TextScale:0.##}×" +
                (snapshot.HighContrast ? " · high contrast" : "") +
                (snapshot.ReduceMotion ? " · reduced motion" : "")));
            content.AddChild(Heading("Semantic colors"));
            foreach (var color in snapshot.Colors)
                content.AddChild(ColorRow(color));

            content.AddChild(Heading("Remote component preview"));
            var primary = snapshot.Colors.FirstOrDefault(color => color.Name == "Primary");
            var secondary = snapshot.Colors.FirstOrDefault(color => color.Name == "Secondary");
            if (primary != null && secondary != null)
            {
                content.AddChild(new HStack()
                    .Spacing(8)
                    .Children(
                        PreviewButton("Primary", primary),
                        PreviewButton("Secondary", secondary)));
            }

            content.AddChild(Heading($"Custom tokens ({snapshot.Tokens.Count})"));
            if (snapshot.Tokens.Count == 0)
                content.AddChild(Muted("No custom ThemeKey values."));
            else
                foreach (var token in snapshot.Tokens)
                    content.AddChild(TokenRow(token));

            content.MarkNeedsLayout();
        }

        _state.ThemeSnapshot.Subscribe(Update);
        Update(_state.ThemeSnapshot.Value);

        return new Frame()
            .Background(DevToolTheme.Colors.Background)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(new ScrollView().Content(content));
    }

    private static Label Heading(string text) =>
        new Label(text)
            .FontSize(13)
            .FontWeight(FontWeight.SemiBold)
            .Foreground(DevToolTheme.Colors.OnBackground);

    private static Label Muted(string text) =>
        new Label(text)
            .FontSize(12)
            .Foreground(DevToolTheme.Colors.OnDisabled);

    private static VisualElement ColorRow(ThemeColorDto color)
    {
        var background = ParseHex(color.Value);
        var foreground = ParseHex(color.OnValue);
        return new Frame()
            .Background(background)
            .BorderRadius(4)
            .Padding(new Thickness(8, 5))
            .Content(new Label(
                $"{color.Name} · {color.Value} · contrast {color.Contrast:0.##}:1")
                .Foreground(foreground));
    }

    private static Button PreviewButton(string text, ThemeColorDto color) =>
        new Button()
            .Text(text)
            .Background(ParseHex(color.Value))
            .TextColor(ParseHex(color.OnValue));

    private static VisualElement TokenRow(ThemeTokenDto token) =>
        token.Color != null
            ? new Frame()
                .Background(ParseHex(token.Color))
                .BorderRadius(4)
                .Padding(new Thickness(8, 5))
                .Content(new Label($"{token.Name} · {token.Color}")
                    .Foreground(ContrastText(ParseHex(token.Color))))
            : new HStack()
                .JustifyContent(JustifyContent.SpaceBetween)
                .Children(
                    Muted(token.Name),
                    Muted($"{token.Value} ({token.ValueType})"));

    private static Color ContrastText(Color background) =>
        Rayo.Styling.ThemeColorUtilities.ContrastRatio(Color.White, background) >= 4.5f
            ? Color.White
            : Color.Black;

    private static Color ParseHex(string value)
    {
        if (value.Length is not (7 or 9) || value[0] != '#')
            return Color.Transparent;
        return new Color(
            Convert.ToByte(value.Substring(1, 2), 16),
            Convert.ToByte(value.Substring(3, 2), 16),
            Convert.ToByte(value.Substring(5, 2), 16),
            value.Length == 9 ? Convert.ToByte(value.Substring(7, 2), 16) : (byte)255);
    }
}
