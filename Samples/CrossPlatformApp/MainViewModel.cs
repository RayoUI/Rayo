using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Reactivity;

namespace CrossPlatformApp;

/// <summary>
/// Main ViewModel for the cross-platform demo application.
/// Contains all business logic that is shared across platforms.
/// </summary>
public class MainViewModel : ViewModelBase
{
    public Signal<string> PlatformName { get; } = new(GetPlatformName());
    public Signal<string> Greeting { get; } = new("Hello from Rayo!");
    public Signal<int> Counter { get; } = new(0);
    public Signal<bool> IsDesktop { get; } = new(PlatformDetector.IsDesktop);
    public Signal<bool> IsMobile { get; } = new(PlatformDetector.IsMobile);

    private static string GetPlatformName()
    {
        return PlatformDetector.CurrentPlatform switch
        {
            PlatformType.Windows => "Windows",
            PlatformType.Linux => "Linux",
            PlatformType.MacOS => "macOS",
            PlatformType.Android => "Android",
            PlatformType.iOS => "iOS",
            PlatformType.WebAssembly => "WebAssembly",
            _ => "Unknown"
        };
    }

    public void IncrementCounter()
    {
        Counter.Value++;
        System.Diagnostics.Debug.WriteLine($"Counter incremented to {Counter.Value}");
    }

    public void DecrementCounter()
    {
        if (Counter.Value > 0)
        {
            Counter.Value--;
            System.Diagnostics.Debug.WriteLine($"Counter decremented to {Counter.Value}");
        }
    }

    public void ResetCounter()
    {
        Counter.Value = 0;
        System.Diagnostics.Debug.WriteLine($"Counter reset to {Counter.Value}");
    }

    public void UpdateGreeting(string newGreeting)
    {
        Greeting.Value = newGreeting;
    }
}
