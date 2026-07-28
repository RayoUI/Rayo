# CrossPlatformApp — Window API demo

Shared UI sample that shows how to configure and mutate the native window
from components on **Desktop**, **Android**, and **iOS** (API surface ready).

## What it demonstrates

### Startup (`configureWindow` / `ConfigureWindow`)

| Platform | Entry point | Examples |
|---|---|---|
| Desktop | [`CrossPlatformApp.Desktop/Program.cs`](../CrossPlatformApp.Desktop/Program.cs) | `Title`, `Width`/`Height`, `WindowState`, `Topmost`, `SystemDecorations`, plus native `StartupLocation` |
| Android | [`CrossPlatformApp.Android/MainActivity.cs`](../CrossPlatformApp.Android/MainActivity.cs) | `KeepScreenOn`, `Orientation`, `ImmersiveMode`, `StatusBarColor`, … via `AndroidWindowConfiguration` |

Defaults live in [`App.CreateDefaultConfiguration()`](App.cs).

### Runtime (from shared components)

```csharp
var window = UIApplication.Current!.Window;

// Desktop
window.Title = "Hello";
window.State = WindowState.Maximized;
window.Topmost = true;
window.SetSize(900, 640);
window.Center();

// Android
window.Android.KeepScreenOn = true;
window.Android.ImmersiveMode = true;
window.Android.Orientation = ScreenOrientation.Portrait;
window.Android.StatusBarColor = 0xFF2563EB;

// iOS (applied when an iOS host registers a controller)
window.iOS.UseSafeAreaInsets = true;
window.iOS.StatusBarStyle = iOSStatusBarStyle.LightContent;
```

The shared [`MainView`](MainView.cs) / [`MainViewModel`](MainViewModel.cs) show the
controls that apply to the current platform (`PlatformDetector`).

## Run

```bash
# Desktop
dotnet run --project Rayo/Samples/CrossPlatformApp.Desktop

# Android (device/emulator via your usual workload)
dotnet build Rayo/Samples/CrossPlatformApp.Android
```

## Mobile safe area

```csharp
var insets = UIApplication.Current!.Window.SafeArea; // Top, Right, Bottom, Left
float top = insets.Top; // 0 on desktop
```

CrossPlatformApp pads content with a top spacer bound to `SafeArea.Top`, and shows the live value in the status card. Toggle immersive / hide status bar on Android to see it update.
