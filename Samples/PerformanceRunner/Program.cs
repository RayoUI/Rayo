using Gallery;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Hosting.Desktop;
using ToDoList;

var scenario = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "gallery";
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
                var backend = Environment.GetEnvironmentVariable("RAYO_DESKTOP_RENDERER") ?? "opengl";
                string label = $"{scenario} ({backend})";
                Console.WriteLine(PerformanceTracker.FormatSummary(label, measuredFrames));
                app.Exit();
            }
        };

        switch (scenario)
        {
            case "gallery":
                context.SetUI<GalleryBuilder>();
                app.ConfigureAssets(assets => Gallery.App.ConfigureAssets(assets));
                break;

            case "todo":
            case "todolist":
                context.SetUI<ToDoApp>();
                break;

            default:
                throw new ArgumentException($"Unknown scenario '{scenario}'. Use 'gallery' or 'todo'.");
        }
    },
    configureWindow: config =>
    {
        config.Title = $"Rayo Performance Runner - {scenario}";
        config.Width = scenario == "gallery" ? 1000 : 370;
        config.Height = scenario == "gallery" ? 700 : 700;
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

static int ParseIntArg(string[] args, int index, int defaultValue)
{
    if (args.Length <= index)
        return defaultValue;

    return int.TryParse(args[index], out var value) && value > 0
        ? value
        : defaultValue;
}
