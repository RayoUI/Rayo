namespace SdlApp;

using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Root UserControl for the SDL2 sample.
/// Demonstrates embedding a live SDL2-rendered 2D mandala inside a Rayo reactive layout.
/// </summary>
public class SdlViewApp : UserControl
{
    // ── Reactive state ────────────────────────────────────────────────────────

    private readonly Signal<float>        _animSpeed  = new(1.0f);
    private readonly Signal<bool>         _animate    = new(true);
    private readonly Signal<int>          _rings      = new(6);
    private readonly Signal<int>          _segments   = new(8);
    private readonly Signal<SdlColorMode> _colorMode  = new(SdlColorMode.Rainbow);

    private readonly Computed<string> _speedLabel;

    // Segment presets: (label, value)
    private static readonly (string Label, int Value)[] SegmentPresets =
    {
        ("△ 3",  3),
        ("⬠ 5",  5),
        ("⬡ 6",  6),
        ("✦ 8",  8),
        ("✺ 12", 12),
    };

    public SdlViewApp()
    {
        _speedLabel = new Computed<string>(() => $"Speed: {_animSpeed.Value:F1}x");
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    public override VisualElement Build() =>
        new VStack()
            .Background(new Color(15, 17, 28))
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                BuildHeader(),
                BuildSceneArea(),
                BuildControlPanel()
            );

    // ── Header ────────────────────────────────────────────────────────────────

    private static VisualElement BuildHeader() =>
        new VStack()
            .Padding(new Thickness(20, 16, 20, 8))
            .Spacing(4)
            .Children(
                new Label("SDL2 Mandala")
                    .FontSize(22)
                    .FontWeight(FontWeight.Bold)
                    .Foreground(Color.White),
                new Label("A live SDL2 geometric scene embedded inside a Rayo reactive control")
                    .FontSize(12)
                    .Foreground(new Color(140, 144, 168))
            );

    // ── Scene area ────────────────────────────────────────────────────────────

    private VisualElement BuildSceneArea() =>
        new Frame()
            .BorderRadius(10)
            .BorderColor(new Color(50, 55, 80))
            .Margin(new Thickness(16, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new SdlView()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Background(new Color(18, 20, 38))
                    .AnimationSpeed(_animSpeed)
                    .Animate(_animate)
                    .Rings(_rings)
                    .Segments(_segments)
                    .ColorMode(_colorMode)
            );

    // ── Control panel ─────────────────────────────────────────────────────────

    private VisualElement BuildControlPanel() =>
        new VStack()
            .Padding(new Thickness(16, 10, 16, 16))
            .Spacing(10)
            .Background(new Color(20, 22, 36))
            .Children(
                new Label("Controls")
                    .FontSize(13)
                    .FontWeight(FontWeight.Bold)
                    .Foreground(new Color(160, 164, 200)),

                // Animation speed row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label()
                            .Text(_speedLabel)
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        new Slider(0.1f, 4.0f, 1.0f)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .TrackFillColor(new Color(99, 102, 241))
                            .OnValueChanged(v => _animSpeed.Value = v)
                    ),

                // Animate toggle row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label()
                            .Text(_animate.Map(a => a ? "Animate: ON" : "Animate: OFF"))
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        new Button()
                            .Text(_animate.Map(a => a ? "Pause" : "Resume"))
                            .Background(new Color(99, 102, 241))
                            .Height(30)
                            .FontSize(12)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .OnTapped(() => _animate.Value = !_animate.Value)
                    ),

                // Ring count row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label()
                            .Text(_rings.Map(r => $"Rings: {r}"))
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        new Slider(2f, 14f, 6f)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .TrackFillColor(new Color(168, 85, 247))
                            .OnValueChanged(v => _rings.Value = (int)MathF.Round(v))
                    ),

                // Segments row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label("Segments:")
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        BuildSegmentButtons()
                    ),

                // Color mode row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label("Color:")
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        BuildColorModeButtons()
                    )
            );

    private VisualElement BuildSegmentButtons()
    {
        var buttons = new List<VisualElement>();
        foreach (var (label, value) in SegmentPresets)
        {
            var v = value;
            buttons.Add(
                new Button()
                    .Text(label)
                    .Background(new Color(40, 44, 70))
                    .Height(30)
                    .FontSize(11)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .OnTapped(() => _segments.Value = v)
            );
        }
        return new HStack()
            .Spacing(6)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(buttons.ToArray());
    }

    private VisualElement BuildColorModeButtons()
    {
        // (label, bg color, mode)
        var presets = new (string Label, Color Bg, SdlColorMode Mode)[]
        {
            ("Rainbow", new Color(139, 92, 246),  SdlColorMode.Rainbow),
            ("Fire",    new Color(220, 80,  20),  SdlColorMode.Fire),
            ("Ocean",   new Color(20,  120, 200), SdlColorMode.Ocean),
            ("Mono",    new Color(80,  80,  100), SdlColorMode.Mono),
        };

        var buttons = new List<VisualElement>();
        foreach (var (label, bg, mode) in presets)
        {
            var m = mode;
            buttons.Add(
                new Button()
                    .Text(label)
                    .Background(bg)
                    .Height(30)
                    .FontSize(11)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .OnTapped(() => _colorMode.Value = m)
            );
        }
        return new HStack()
            .Spacing(6)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(buttons.ToArray());
    }
}
