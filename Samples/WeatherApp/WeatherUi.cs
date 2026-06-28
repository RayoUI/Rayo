using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Gestures.Components;
using Rayo.Layout;
using Rayo.Rendering;

namespace WeatherApp;

internal static class WeatherUi
{
    public const string AssetRoot = "Assets/Images/";

    public static Color Background(bool dark) => dark ? new Color(5, 22, 31) : new Color(232, 238, 242);
    public static Color Surface(bool dark) => dark ? new Color(18, 45, 59) : Color.White;
    public static Color SurfaceAlt(bool dark) => dark ? new Color(25, 57, 73) : new Color(214, 224, 230);
    public static Color Text(bool dark) => dark ? Color.White : new Color(28, 34, 38);
    public static Color Muted(bool dark) => dark ? new Color(145, 167, 183) : new Color(82, 101, 114);
    public static Color Accent => new Color(62, 142, 237);
    public static Color Gold => new Color(247, 181, 72);
    public static Color Purple => new Color(81, 43, 212);

    public static Image WeatherImage(string file, float size) =>
        new Image()
            .Source(AssetRoot + file)
            .Size(size)
            .Stretch(StretchMode.Uniform);

    public static Label Heading(string text, bool dark) =>
        new Label(text).FontSize(25).Foreground(Accent);

    public static Frame Card(bool dark, VisualElement content) =>
        new Frame()
            .Background(Surface(dark))
            .BorderBrush(SurfaceAlt(dark))
            .BorderThickness(1)
            .BorderRadius(20)
            .Margin(new Thickness(4))
            .Padding(new Thickness(18))
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(content);

    public static VisualElement NavButton(
        string icon,
        string selectedIcon,
        bool selected,
        bool dark,
        Action tapped)
    {
        var surface = new Frame()
            .Size(52)
            .Padding(new Thickness(11))
            .Background(selected ? SurfaceAlt(dark) : Color.Transparent)
            .BorderThickness(0)
            .BorderRadius(10)
            .Content(WeatherImage(selected ? selectedIcon : icon, 30));

        return new GestureDetector(surface)
            .OnTap((System.Numerics.Vector2 _) => tapped());
    }
}
