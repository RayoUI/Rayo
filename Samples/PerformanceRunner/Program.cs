using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Hosting.Desktop;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;

var scenario = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "scroll";
int warmupFrames = ParseIntArg(args, 1, 60);
int measuredFrames = ParseIntArg(args, 2, 120);

var host = new DesktopPlatformHost();
int frameCounter = 0;
bool measuringStarted = false;

host.Run(
    configureApp: context =>
    {
        var app = host.GetNativeApplication(context)
                  ?? throw new InvalidOperationException("Native UIApplication is required for benchmarks.");

        PerformanceTracker.IsEnabled = true;
        PerformanceTracker.ClearDirtyLog();
        PerformanceTracker.ClearFrameHistory();

        app.ContinuousRendering = true;
        app.Updated += _ =>
        {
            DriveScenario(scenario, app, frameCounter);
            frameCounter++;

            if (!measuringStarted && frameCounter >= warmupFrames)
            {
                measuringStarted = true;
                PerformanceTracker.ClearFrameHistory();
                PerformanceTracker.ClearDirtyLog();
                frameCounter = 0;
            }

            if (measuringStarted && frameCounter >= measuredFrames)
            {
                var backend = Environment.GetEnvironmentVariable("RAYO_DESKTOP_RENDERER") ?? "skia";
                string label = $"{scenario} ({backend})";
                Console.WriteLine(PerformanceTracker.FormatSummary(label, measuredFrames));
                app.Exit();
            }
        };

        switch (scenario)
        {
            case "scroll":
            case "scrollview":
                context.SetUI<BenchmarkScrollPage>();
                break;

            case "editor":
            case "text":
                context.SetUI<BenchmarkEditorPage>();
                break;

            case "editor-wrap":
            case "text-wrap":
                context.SetUI<BenchmarkWrappedEditorPage>();
                break;

            case "styles":
            case "style":
                context.SetUI<BenchmarkStylesPage>();
                break;

            default:
                throw new ArgumentException($"Unknown scenario '{scenario}'. Use 'scroll', 'editor', 'editor-wrap' or 'styles'.");
        }
    },
    configureWindow: config =>
    {
        config.Title = $"Rayo Performance Runner - {scenario}";
        config.Width = scenario switch
        {
            "editor" or "text" or "editor-wrap" or "text-wrap" => 900,
            "styles" or "style" => 1180,
            _ => 1000
        };
        config.Height = scenario switch
        {
            "editor" or "text" or "editor-wrap" or "text-wrap" => 760,
            "styles" or "style" => 820,
            _ => 700
        };
        config.CanResize = false;
        config.VSync = false;

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.TargetFPS = 240;
            nativeConfig.StartupLocation = WindowStartupLocation.Manual;
            nativeConfig.X = 40;
            nativeConfig.Y = 40;
        }
    });

static void DriveScenario(string scenario, UIApplication app, int frameCounter)
{
    switch (scenario)
    {
        case "scroll":
        case "scrollview":
            {
                var scrollView = BenchmarkScrollPage.ActiveScrollView;
                if (scrollView == null)
                    return;

                const float amplitude = 18f;
                float nextOffset = scrollView.VerticalScrollOffset + amplitude;
                if (nextOffset >= scrollView.ContentHeight)
                {
                    scrollView.VerticalScrollOffset = 0;
                }
                else
                {
                    scrollView.VerticalScrollOffset = nextOffset;
                }
                break;
            }

        case "editor":
        case "text":
            {
                var editor = BenchmarkEditorPage.ActiveEditor;
                if (editor == null)
                    return;

                float phase = frameCounter * 14f;
                editor.SetVerticalScrollOffset(phase);
                break;
            }

        case "editor-wrap":
        case "text-wrap":
            {
                var editor = BenchmarkWrappedEditorPage.ActiveEditor;
                if (editor == null)
                    return;

                float phase = frameCounter * 10f;
                editor.SetVerticalScrollOffset(phase);
                break;
            }

        case "styles":
        case "style":
            BenchmarkStylesPage.DriveFrame(frameCounter);
            break;
    }
}

static int ParseIntArg(string[] args, int index, int defaultValue)
{
    if (args.Length <= index)
        return defaultValue;

    return int.TryParse(args[index], out var value) && value > 0
        ? value
        : defaultValue;
}

internal sealed class BenchmarkScrollPage : Component
{
    internal static ScrollView? ActiveScrollView { get; private set; }

    public override VisualElement Build()
    {
        var items = new VStack()
            .Spacing(12)
            .Padding(new Thickness(16));

        for (int i = 0; i < 220; i++)
        {
            items.AddChild(
                new Frame()
                    .Background(i % 2 == 0 ? new Color(36, 40, 54) : new Color(28, 32, 44))
                    .BorderRadius(10)
                    .Padding(new Thickness(14))
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(
                        new VStack()
                            .Spacing(6)
                            .Children(
                                new Label($"Scrollable row {i + 1}")
                                    .FontSize(16)
                                    .Foreground(Color.White),
                                new Label($"Benchmark content line {i + 1} with alternating colors, nested labels and enough text to exercise rasterization while the viewport moves continuously.")
                                    .Foreground(new Color(170, 178, 196))
                            )
                    )
            );
        }

        var scrollView = new ScrollView()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(items);

        ActiveScrollView = scrollView;

        return new Frame()
            .Background(new Color(18, 20, 28))
            .Padding(new Thickness(18))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                scrollView
            );
    }
}

internal sealed class BenchmarkEditorPage : Component
{
    internal static Editor? ActiveEditor { get; private set; }

    public override VisualElement Build()
    {
        var editor = new Editor()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Height(620)
            .FontSize(14)
            .Background(new Color(16, 18, 24))
            .TextColor(new Color(200, 224, 255))
            .BorderBrush(new Color(50, 58, 74))
            .WordWrap(false)
            .Text(BuildEditorText());

        ActiveEditor = editor;

        return new Frame()
            .Background(new Color(18, 20, 28))
            .Padding(new Thickness(18))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(12)
                    .Children(
                        new Label("Editor benchmark")
                            .FontSize(18)
                            .Foreground(Color.White),
                        new Label("Large multiline text with continuous vertical scrolling to stress text measurement and rendering.")
                            .Foreground(new Color(160, 170, 190)),
                        editor
                    )
            );
    }

    private static string BuildEditorText()
    {
        var lines = new System.Text.StringBuilder(capacity: 64 * 1024);
        for (int i = 1; i <= 900; i++)
        {
            lines.Append("Line ")
                .Append(i.ToString("D4"))
                .Append(": RayoUI benchmark text with repeated glyphs, numbers 1234567890, punctuation []{}()<>, and wider words for horizontal metrics.")
                .Append('\n');
        }

        return lines.ToString();
    }
}

internal sealed class BenchmarkWrappedEditorPage : Component
{
    internal static Editor? ActiveEditor { get; private set; }

    public override VisualElement Build()
    {
        var editor = new Editor()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Height(620)
            .FontSize(14)
            .Background(new Color(16, 18, 24))
            .TextColor(new Color(235, 239, 255))
            .BorderBrush(new Color(50, 58, 74))
            .WordWrap(true)
            .Text(BuildWrappedEditorText());

        ActiveEditor = editor;

        return new Frame()
            .Background(new Color(18, 20, 28))
            .Padding(new Thickness(18))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(12)
                    .Children(
                        new Label("Wrapped editor benchmark")
                            .FontSize(18)
                            .Foreground(Color.White),
                        new Label("Long paragraphs with word wrap enabled and continuous vertical scrolling to stress wrapped line building and text rasterization.")
                            .Foreground(new Color(160, 170, 190)),
                        editor
                    )
            );
    }

    private static string BuildWrappedEditorText()
    {
        var text = new System.Text.StringBuilder(capacity: 96 * 1024);
        for (int i = 1; i <= 420; i++)
        {
            text.Append("Paragraph ")
                .Append(i.ToString("D3"))
                .Append(": RayoUI wrapped editor benchmark with deliberately long sentences that should flow across multiple visual lines, mixing punctuation, 1234567890, symbols []{}()<>, and repeated phrases to exercise line breaking, cached measurements, and continuous scrolling under word wrap. ");
            text.Append("This paragraph continues with a second sentence so the renderer cannot rely on only short fragments and has to keep drawing longer wrapped runs in the viewport. ");
            text.Append("Tabs\tare\tincluded\toccasionally to ensure the processed display text path stays exercised.")
                .Append('\n');
        }

        return text.ToString();
    }
}

internal sealed class BenchmarkStylesPage : Component
{
    private static readonly List<Frame> s_cards = [];
    private static readonly List<Label> s_metaLabels = [];
    private static readonly List<Button> s_buttons = [];
    private const int AnimatedBatchSize = 12;

    protected override StyleSheet? BuildStyles() =>
    [
        new Style<Frame>(".bench-card")
            .Background(new Color(28, 32, 44))
            .BorderBrush(new Color(46, 54, 72))
            .BorderThickness(1f)
            .BorderRadius(12f),

        new Style<Frame>(".bench-card.active")
            .Background(new Color(42, 54, 78))
            .BorderBrush(new Color(104, 154, 255))
            .BorderThickness(2f),

        new Style<Frame>(".bench-card.accent")
            .Background(new Color(46, 36, 62)),

        new Style<Label>(".bench-title")
            .Foreground(Color.White)
            .FontSize(15f),

        new Style<Label>(".bench-meta")
            .Foreground(new Color(164, 174, 196))
            .FontSize(12f),

        new Style<Label>(".bench-meta.muted")
            .Foreground(new Color(124, 132, 152)),

        new Style<Button>(".bench-btn")
            .Height(30f)
            .BorderRadius(8f)
            .Background(new Color(64, 74, 94))
            .HoverBackground(new Color(74, 84, 108))
            .PressedBackground(new Color(52, 62, 80))
            .TextColor(Color.White),

        new Style<Button>(".bench-btn.accent")
            .Background(new Color(76, 112, 208))
            .HoverBackground(new Color(88, 124, 224))
            .PressedBackground(new Color(62, 96, 182))
    ];

    public override VisualElement Build()
    {
        s_cards.Clear();
        s_metaLabels.Clear();
        s_buttons.Clear();

        var grid = new Grid()
            .Columns(
                GridLength.Star,
                GridLength.Star,
                GridLength.Star,
                GridLength.Star)
            .ColumnSpacing(14)
            .RowSpacing(14)
            .Padding(new Thickness(16));

        for (int i = 0; i < 160; i++)
        {
            var title = new Label($"Style card {i + 1}")
                .Classes("bench-title");

            var meta = new Label($"Class toggles, hover-like state and button skin benchmark row {i + 1}.")
                .Classes("bench-meta");

            var button = new Button()
                .Text(i % 3 == 0 ? "Primary action" : "Inspect")
                .Classes("bench-btn");

            var card = new Frame()
                .Classes("bench-card")
                .Padding(new Thickness(12))
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Content(
                    new VStack()
                        .Spacing(10)
                        .Children(title, meta, button)
                );

            s_cards.Add(card);
            s_metaLabels.Add(meta);
            s_buttons.Add(button);

            grid.AddChild(card, i / 4, i % 4);
        }

        return new Frame()
            .Background(new Color(18, 20, 28))
            .Padding(new Thickness(18))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(12)
                    .Children(
                        new Label("Styles benchmark")
                            .FontSize(18)
                            .Foreground(Color.White),
                        new Label("Large grid with continuous class changes across cards, labels and buttons to stress incremental style re-application.")
                            .Foreground(new Color(160, 170, 190)),
                        new ScrollView()
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch)
                            .Content(grid)
                    )
            );
    }

    internal static void DriveFrame(int frameCounter)
    {
        if (s_cards.Count == 0)
            return;

        int count = s_cards.Count;
        int currentStart = (frameCounter * AnimatedBatchSize) % count;
        int previousStart = (((frameCounter - 1 + count) % count) * AnimatedBatchSize) % count;

        ApplyBatch(previousStart, isActive: false, accent: false);
        ApplyBatch(currentStart, isActive: true, accent: (frameCounter & 1) == 0);
    }

    private static void ApplyBatch(int startIndex, bool isActive, bool accent)
    {
        for (int offset = 0; offset < AnimatedBatchSize; offset++)
        {
            int index = (startIndex + offset) % s_cards.Count;
            var card = s_cards[index];
            var meta = s_metaLabels[index];
            var button = s_buttons[index];

            if (isActive)
            {
                card.AddClass("active");
                if (accent && (index & 1) == 0)
                    card.AddClass("accent");
                else
                    card.RemoveClass("accent");

                button.AddClass("accent");
                meta.AddClass("muted");
            }
            else
            {
                card.RemoveClass("active");
                card.RemoveClass("accent");
                button.RemoveClass("accent");
                meta.RemoveClass("muted");
            }
        }
    }
}
