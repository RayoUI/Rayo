using Microsoft.Extensions.DependencyInjection;
using Rayo.Styling;

namespace NanoApp;

public static class AppSetup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        RayoThemes.UseTheme(RayoThemes.Dark);
    }
}
