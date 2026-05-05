namespace Rayo.Core;

using Rayo.Core.Interfaces;
using System.Runtime.CompilerServices;

/// <summary>
/// Hot reload manager that integrates with .NET Hot Reload mechanism.
/// </summary>
public class HotReloadManager : IDisposable
{
    private readonly UIApplication _app;
    private Type? _builderType;
    private static HotReloadManager? _instance;
    private DateTime _lastReload = DateTime.MinValue;
    private const int ReloadDebounceMs = 200;

    public bool IsEnabled { get; private set; }

    public event Action? OnReloaded;

    public HotReloadManager(UIApplication app)
    {
        _app = app;
        _instance = this;
    }

    /// <summary>
    /// This method is called from MetadataUpdateHandler when .NET Hot Reload applies changes.
    /// </summary>
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        if (_instance is not null && _instance.IsEnabled)
        {
            // Execute reload on the main thread
            _instance._app.RunOnMainThread(() => _instance.ReloadUI());
        }
    }

    /// <summary>
    /// Enables hot reload for a specific UI builder.
    /// </summary>
    /// <typeparam name="T">The type implementing IUIBuilder.</typeparam>
    public void Enable<T>() where T : IUIBuilder, new()
    {
        _builderType = typeof(T);
        IsEnabled = true;

        // Initial UI load
        ReloadUI();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReloadUI()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastReload).TotalMilliseconds < ReloadDebounceMs)
        {
            return; // Avoid multiple reloads in short succession
        }
        _lastReload = now;

        if (_builderType == null)
        {
            Console.WriteLine("[HotReload] Error: UI builder type is not defined.");
            return;
        }

        try
        {
            // .NET Hot Reload has already updated the code in memory.
            // We simply need to create a new instance of the updated type.
            if (Activator.CreateInstance(_builderType) is not IUIBuilder builder)
            {
                Console.WriteLine("[HotReload] Error: Could not create instance of UI builder.");
                return;
            }

            VisualElement newRoot;
            
            // If the builder is a Component, use the component itself as the root
            // This allows the root component to have state, lifecycle and Rebuild capabilities
            if (builder is UserControl component)
            {
                newRoot = component;
            }
            else
            {
                // Legacy/Standard builder: just get the built tree
                newRoot = builder.Build();
            }

            _app.Tree.SetRoot(newRoot);
            _app.Tree.MarkNeedsLayout();
            _app.Tree.MarkNeedsRender();

            OnReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotReload] Error reloading UI: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    public void Dispose()
    {
        IsEnabled = false;
        _instance = null;
    }
}