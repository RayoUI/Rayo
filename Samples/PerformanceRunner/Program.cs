using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Hosting.Desktop;
using Rayo.Layout;
using Rayo.Rendering;

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

            default:
                throw new ArgumentException($"Unknown scenario '{scenario}'. Use 'scroll' or 'editor'.");
        }
    },
    configureWindow: config =>
    {
        config.Title = $"Rayo Performance Runner - {scenario}";
        config.Width = scenario switch
        {
            "editor" or "text" => 900,
            _ => 1000
        };
        config.Height = scenario switch
        {
            "editor" or "text" => 760,
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

internal sealed class BenchmarkScrollPage : UserControl
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

internal sealed class BenchmarkEditorPage : UserControl
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
            .BorderColor(new Color(50, 58, 74))
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
