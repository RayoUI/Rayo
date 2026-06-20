using Rayo.Core.Assets;
using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

namespace FontSymbolsViewer;

public static class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
#if DEBUG
                context.EnableDevTools = true;
#endif
                if (host.GetNativeApplication(context) is { } app)
                {
                    app.ConfigureAssets(ConfigureAssets);
                }

                context.SetUI<FontSymbolsApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Font Symbols Viewer";
                config.Width = 1060;
                config.Height = 760;
                config.CanResize = true;
                config.VSync = true;
                config.Samples = 4;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                {
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
        );
    }

    private static void ConfigureAssets(AssetConfiguration assets)
    {
        assets.AddSearchPath("Assets");
        assets.ConfigureFonts(fonts =>
        {
            fonts.AddFont("Fonts/Lineicons.ttf", FontSymbolsApp.FontAlias, 42);
        });
    }
}
