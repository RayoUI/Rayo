using Rayo;
using Rayo.Animation;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class AnimationPage : UserControl
{
    private readonly List<EasingDemoState> _easingDemos = new();
    private bool _isPlayingAll;
    private int _activeAnimations;
    private const float TrackWidth = 350f;
    private const float IndicatorSize = 24f;
    private const float TrackContainerWidth = TrackWidth + IndicatorSize;
    private const float TrackHeight = 28f;
    private const float AnimationDurationMs = 1500f;

    public override VisualElement Build()
    {
        _easingDemos.Clear();

        var content = new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Animation & Easing", "Built-in easing functions for smooth animations"),

                CreateControlFrame(),

                Helper.CreateExampleSection("Linear & Quad",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            CreateEasingDemo("Linear", Easing.Linear),
                            CreateEasingDemo("InQuad", Easing.InQuad),
                            CreateEasingDemo("OutQuad", Easing.OutQuad),
                            CreateEasingDemo("InOutQuad", Easing.InOutQuad)
                        )
                ),

                Helper.CreateExampleSection("Cubic & Quart",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            CreateEasingDemo("InCubic", Easing.InCubic),
                            CreateEasingDemo("OutCubic", Easing.OutCubic),
                            CreateEasingDemo("InOutCubic", Easing.InOutCubic),
                            CreateEasingDemo("OutQuart", Easing.OutQuart)
                        )
                ),

                Helper.CreateExampleSection("Sine & Expo",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            CreateEasingDemo("InSine", Easing.InSine),
                            CreateEasingDemo("OutSine", Easing.OutSine),
                            CreateEasingDemo("InExpo", Easing.InExpo),
                            CreateEasingDemo("OutExpo", Easing.OutExpo)
                        )
                ),

                Helper.CreateExampleSection("Back & Elastic",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            CreateEasingDemo("InBack", Easing.InBack),
                            CreateEasingDemo("OutBack", Easing.OutBack),
                            CreateEasingDemo("OutElastic", Easing.OutElastic),
                            CreateEasingDemo("Spring", Easing.Spring)
                        )
                ),

                Helper.CreateExampleSection("Bounce",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            CreateEasingDemo("InBounce", Easing.InBounce),
                            CreateEasingDemo("OutBounce", Easing.OutBounce),
                            CreateEasingDemo("InOutBounce", Easing.InOutBounce)
                        )
                ),

                Helper.CreateExampleSection("Easing Curves Visualization",
                    CreateCurvesVisualization()
                )
            );

        return content;
    }

    private VisualElement CreateControlFrame()
    {
        return new Frame()
            .Background(new Color(35, 35, 40))
            .BorderRadius(8)
            .Padding(new Thickness(16))
            .Content(
                new HStack()
                    .Spacing(16)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Button()
                            .Text("Play All Animations")
                            .Background(new Color(59, 130, 246))
                            .HoverBackground(new Color(79, 150, 255))
                            .TextColor(Color.White)
                            .BorderWidth(0)
                            .Padding(new Thickness(20, 12, 20, 12))
                            .OnTapped(PlayAllAnimations),

                        new Button()
                            .Text("Reset")
                            .Background(new Color(100, 105, 120))
                            .HoverBackground(new Color(120, 125, 140))
                            .TextColor(Color.White)
                            .BorderWidth(0)
                            .Padding(new Thickness(20, 12, 20, 12))
                            .OnTapped(ResetAllAnimations),

                        new Label("Click 'Play All' to see easing functions in action")
                            .FontSize(13)
                            .Foreground(new Color(140, 145, 160))
                    )
            );
    }

    private VisualElement CreateEasingDemo(string name, Func<float, float> easingFunc)
    {
        var indicator = new Frame();
        indicator.Size(new Size(IndicatorSize, IndicatorSize));
        indicator.Background(new Color(59, 130, 246));
        indicator.BorderRadius(12);

        var demoState = new EasingDemoState(name, easingFunc, indicator);
        _easingDemos.Add(demoState);

        return new HStack()
            .Spacing(12)
            .Alignment(Alignment.Center)
            .Children(
                new Label(name)
                    .Width(100)
                    .FontSize(13)
                    .Foreground(new Color(180, 185, 195)),

                new Frame()
                    .Height(TrackHeight)
                    .Width(TrackContainerWidth)
                    .Background(new Color(35, 38, 48))
                    .BorderRadius(4)
                    .Content(
                        new Absolute()
                            .Size(new Size(TrackContainerWidth, TrackHeight))
                            .Children(indicator)
                    ),

                new HStack()
                    .Spacing(8)
                    .Children(
                        new Button()
                            .Text("Play")
                            .Background(new Color(59, 130, 246))
                            .HoverBackground(new Color(79, 150, 255))
                            .TextColor(Color.White)
                            .BorderWidth(0)
                            .Padding(new Thickness(12, 8, 12, 8))
                            .OnTapped(() => PlayAnimation(demoState)),

                        new Button()
                            .Text("Reset")
                            .Background(new Color(100, 105, 120))
                            .HoverBackground(new Color(120, 125, 140))
                            .TextColor(Color.White)
                            .BorderWidth(0)
                            .Padding(new Thickness(12, 8, 12, 8))
                            .OnTapped(() => ResetAnimation(demoState))
                    )
            );
    }

    private VisualElement CreateCurvesVisualization()
    {
        return new HStack()
            .Spacing(20)
            .Children(
                CreateCurveGraph("OutQuad", Easing.OutQuad, new Color(59, 130, 246)),
                CreateCurveGraph("OutCubic", Easing.OutCubic, new Color(34, 197, 94)),
                CreateCurveGraph("OutElastic", Easing.OutElastic, new Color(168, 85, 247)),
                CreateCurveGraph("OutBounce", Easing.OutBounce, new Color(239, 68, 68))
            );
    }

    private VisualElement CreateCurveGraph(string name, Func<float, float> easingFunc, Color color)
    {
        const int width = 100;
        const int height = 80;
        const int steps = 20;

        var canvas = new Absolute()
            .Size(new Size(width, height))
            .Background(new Color(30, 33, 42));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float value = easingFunc(t);

            float x = t * (width - 8) + 4;
            float y = height - 4 - (value * (height - 8));

            y = Math.Clamp(y, 2, height - 2);

            var point = new Frame()
                .AbsolutePosition(x - 2, y - 2)
                .Size(new Size(4, 4))
                .Background(color)
                .BorderRadius(2);

            canvas.AddChild(point);
        }

        return new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                canvas,
                new Label(name)
                    .FontSize(11)
                    .Foreground(color)
            );
    }

    private void PlayAllAnimations()
    {
        if (_isPlayingAll) return;
        _isPlayingAll = true;

        int remaining = _easingDemos.Count;
        foreach (var demo in _easingDemos)
        {
            PlayAnimation(demo, onComplete: () =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                    _isPlayingAll = false;
            });
        }
    }

    private void PlayAnimation(EasingDemoState demo, Action? onComplete = null)
    {
        if (demo.IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        demo.IsAnimating = true;
        BeginAnimation();

        var anim = new DemoAnimation(demo, TrackWidth, AnimationDurationMs, () =>
        {
            demo.IsAnimating = false;
            EndAnimation();
            onComplete?.Invoke();
        });

        FrameAnimationTicker.Register(anim);
    }

    private void ResetAllAnimations()
    {
        foreach (var demo in _easingDemos)
        {
            ResetAnimation(demo);
        }
    }

    private void ResetAnimation(EasingDemoState demo)
    {
        demo.Indicator.X(0);
        demo.Indicator.MarkNeedsPaint();
    }

    private void BeginAnimation()
    {
        if (_activeAnimations == 0)
            ContinuousRendering(true);

        _activeAnimations++;
    }

    private void EndAnimation()
    {
        _activeAnimations = Math.Max(0, _activeAnimations - 1);
        if (_activeAnimations == 0)
            ContinuousRendering(false);
    }

    private static void ContinuousRendering(bool enabled)
    {
        var app = UIApplication.Current;
        if (app != null)
            app.ContinuousRendering = enabled;
    }

    private sealed class DemoAnimation : IFrameAnimation
    {
        private readonly EasingDemoState _demo;
        private readonly float _trackWidth;
        private readonly float _durationMs;
        private readonly Action _onComplete;
        private float _elapsedMs;

        public DemoAnimation(EasingDemoState demo, float trackWidth, float durationMs, Action onComplete)
        {
            _demo = demo;
            _trackWidth = trackWidth;
            _durationMs = durationMs;
            _onComplete = onComplete;
        }

        public void Tick(float deltaTime)
        {
            _elapsedMs += deltaTime * 1000f;
            float t = Math.Min(1f, _elapsedMs / _durationMs);

            float easedT = _demo.Easing(t);
            _demo.Indicator.X(easedT * _trackWidth);
            _demo.Indicator.MarkNeedsPaint();

            if (t >= 1f)
            {
                FrameAnimationTicker.Unregister(this);
                _onComplete();
            }
        }
    }

    private sealed class EasingDemoState
    {
        public EasingDemoState(string name, Func<float, float> easing, Frame indicator)
        {
            Name = name;
            Easing = easing;
            Indicator = indicator;
        }

        public string Name { get; }
        public Func<float, float> Easing { get; }
        public Frame Indicator { get; }
        public bool IsAnimating { get; set; }
    }
}
