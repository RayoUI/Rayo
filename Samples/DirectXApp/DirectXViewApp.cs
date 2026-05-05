namespace DirectXApp;

using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Root UserControl for the Direct3D 11 sample.
/// Demonstrates embedding a live 3D scene inside a Rayo reactive layout.
/// DirectXScene3D owns its own D3D11 device — the UI itself uses SkiaSharp.
/// </summary>
public class DirectXViewApp : UserControl
{
    // ── Reactive state ────────────────────────────────────────────────────────

    private readonly Signal<float> _animSpeed = new(1.0f);
    private readonly Signal<bool>  _animate   = new(true);
    private readonly Signal<Brush> _cubeColor = new(new Color(0.3f, 0.6f, 1.0f));
    private readonly Signal<int>   _maxFps    = new(60);

    private readonly Computed<string> _speedLabel;

    // Preset colours for the cube
    private static readonly (string Label, Color Color)[] ColorPresets =
    {
        ("Blue",   new Color(0.3f, 0.6f, 1.0f)),
        ("Green",  new Color(0.2f, 0.9f, 0.4f)),
        ("Orange", new Color(1.0f, 0.5f, 0.1f)),
        ("Pink",   new Color(0.9f, 0.2f, 0.7f)),
    };

    public DirectXViewApp()
    {
        _speedLabel = new Computed<string>(() => $"Speed: {_animSpeed.Value:F1}x");
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    public override VisualElement Build() =>
        new VStack()
            .Background(new Color(12, 14, 22))
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
                new Label("Direct3D 11 Cube")
                    .FontSize(22)
                    .FontWeight(FontWeight.Bold)
                    .Foreground(Color.White),
                new Label("A live 3D scene rendered with an independent D3D11 device")
                    .FontSize(12)
                    .Foreground(new Color(140, 144, 168))
            );

    // ── Scene area ────────────────────────────────────────────────────────────

    private VisualElement BuildSceneArea() =>
        new Frame()
            .BorderRadius(10)
            .BorderColor(new Color(45, 50, 80))
            .Margin(new Thickness(16, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new DirectXView()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Background(new Color(15, 18, 32))
                    .AnimationSpeed(_animSpeed)
                    .Animate(_animate)
                    .CubeColor(_cubeColor)
                    .MaxFps(_maxFps)
            );

    // ── Control panel ─────────────────────────────────────────────────────────

    private VisualElement BuildControlPanel() =>
        new VStack()
            .Padding(new Thickness(16, 10, 16, 16))
            .Spacing(10)
            .Background(new Color(18, 20, 32))
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
                            .TrackFillColor(new Color(0, 120, 215))
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
                            .Background(new Color(0, 120, 215))
                            .Height(30)
                            .FontSize(12)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .OnTapped(() => _animate.Value = !_animate.Value)
                    ),

                // Color presets row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label("Color:")
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        BuildColorPresets()
                    )
            );

    private VisualElement BuildColorPresets()
    {
        var buttons = new List<VisualElement>();
        foreach (var (label, color) in ColorPresets)
        {
            var c = color;
            buttons.Add(
                new Button()
                    .Text(label)
                    .Background(new Color(c.R, c.G, c.B))
                    .Height(30)
                    .FontSize(11)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .OnTapped(() => _cubeColor.Value = c)
            );
        }
        return new HStack()
            .Spacing(6)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(buttons.ToArray());
    }
}
