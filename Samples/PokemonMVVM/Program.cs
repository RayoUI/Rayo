using Microsoft.Extensions.DependencyInjection;
using Pokemon;
using Pokemon.Services;
using Pokemon.Services.Interfaces;
using Rayo.Hosting.Desktop;
using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

var host = new DesktopPlatformHost();

host.Run(
    configureApp: context =>
    {
        // Configure services
        context.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IPokeService, PokeService>();
            services.AddTransient<PokemonViewModel>();
        });

#if DEBUG
        context.EnableDevTools = true;
#endif
        context.SetUI<PokemonView>();
    },
    configureWindow: config =>
    {
        config.Title = "Pokemon App";
        config.Width = 800;
        config.Height = 600;
        config.CanResize = true;
        config.VSync = true;
        
        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
            nativeConfig.Topmost = true;
        }
    }
);

