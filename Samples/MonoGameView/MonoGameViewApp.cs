namespace MonoGameSample;

using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// Root UserControl for the MonoGame sample.
/// Demonstrates embedding a live MonoGame scene inside a Rayo reactive layout.
/// </summary>
public class MonoGameViewApp : UserControl
{
    // ── Reactive state ─────────────────────────────────────────────────────────
    private readonly Signal<float> _animSpeed = new(1.0f);
    private readonly Signal<int>   _ballCount = new(5);

    private readonly Computed<string>  _speedLabel;
    private readonly Computed<string>  _countLabel;

    public MonoGameViewApp()
    {
        _speedLabel = new Computed<string>(() => $"Speed: {_animSpeed.Value:F1}x");
        _countLabel = new Computed<string>(() => $"Balls: {_ballCount.Value}");
    }

    // ── Build ──────────────────────────────────────────────────────────────────
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

    // ── Header ─────────────────────────────────────────────────────────────────
    private static VisualElement BuildHeader() =>
        new VStack()
            .Padding(new Thickness(20, 16, 20, 8))
            .Spacing(4)
            .Children(
                new Label()
                    .Text("MonoGame  +  Rayo")
                    .FontSize(22)
                    .FontWeight(FontWeight.Bold)
                    .Foreground(Color.White),
                new Label()
                    .Text("A live MonoGame scene embedded inside a Rayo reactive control")
                    .FontSize(12)
                    .Foreground(new Color(140, 144, 168))
            );

    // ── MonoGame viewport ──────────────────────────────────────────────────────
    private VisualElement BuildSceneArea() =>
        new Frame()
            .BorderRadius(10)
            //.BorderWidth(1)
            .BorderColor(new Color(50, 55, 80))
            .Margin(new Thickness(16, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new MonoGameView()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Background(new Color(18, 20, 38))
                    .AnimSpeed(_animSpeed)
                    .BallCount(_ballCount)
            );

    // ── Control panel ──────────────────────────────────────────────────────────
    private VisualElement BuildControlPanel() =>
        new VStack()
            .Padding(new Thickness(16, 10, 16, 16))
            .Spacing(10)
            .Background(new Color(20, 22, 36))
            .Children(
                new Label()
                    .Text("Controls")
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

                // Ball count row
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label()
                            .Text(_countLabel)
                            .Width(95)
                            .FontSize(13)
                            .Foreground(Color.White),
                        new Slider(1f, 30f, 5f)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .TrackFillColor(new Color(236, 72, 153))
                            .OnValueChanged(v => _ballCount.Value = (int)MathF.Round(v))
                    )
            );
}
