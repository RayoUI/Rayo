using Rayo.DevTool.Shared.Protocol;
using Rayo.Reactivity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rayo.DevTool;

public class DevToolState : System.IDisposable
{
    public DevToolClient Client { get; } = new();

    public Signal<ElementNode?> RootNode { get; } = new(null);
    public Signal<List<ElementNode>> OverlayNodes { get; } = new(new List<ElementNode>());
    public Signal<string?> SelectedElementId { get; } = new(null);
    public Signal<List<PropertyInfo>> Properties { get; } = new(new List<PropertyInfo>());
    public Signal<bool> IsConnected { get; } = new(false);
    public Signal<string> ConnectionStatus { get; } = new("Disconnected");
    public Signal<string> Host { get; } = new("localhost");
    public Signal<int> Port { get; } = new(9999);

    public Signal<List<LogMessage>> Logs { get; } = new(new List<LogMessage>());
    public Signal<bool> IsConsoleMaximized    { get; } = new(true);
    public Signal<bool> IsHighlightEnabled    { get; } = new(true);

    // Performance panel
    public Signal<bool> IsPerformancePanelOpen  { get; } = new(false);
    public Signal<bool> IsDirtyHeatmapEnabled   { get; } = new(false);
    public Signal<bool> IsOverdrawEnabled        { get; } = new(false);
    public Signal<bool> IsExtendedStatsEnabled   { get; } = new(false);
    public Signal<PerformanceStatsResponse?> PerformanceStats { get; } = new(null);
    public Signal<List<DirtyEntryDto>> DirtyLog { get; } = new(new List<DirtyEntryDto>());

    // Persist expanded state across tree rebuilds (keyed by element ID)
    public Dictionary<string, bool> ExpandedStates { get; } = new();

    private bool _manualDisconnect = false;
    private CancellationTokenSource _reconnectCts = new();

    private static string Ts => System.DateTime.Now.ToString("HH:mm:ss.fff");
    private static void Log(string msg) => Console.WriteLine($"[DevTool State {Ts}] {msg}");

    public DevToolState()
    {
        Client.Connected += () =>
        {
            Log("Connected event received. Setting IsConnected=true, starting RefreshTree.");
            _manualDisconnect = false;
            IsConnected.Value = true;
            ConnectionStatus.Value = $"Connected to {Host.Value}:{Port.Value}";
            _ = RefreshTreeAsync();

            // If Record was already ON before this (re)connect, restart the poll loop.
            // The IsPerformancePanelOpen subscription won't re-fire because the value didn't change.
            if (IsPerformancePanelOpen.Value)
            {
                IsExtendedStatsEnabled.Value = true;
                _ = PollPerformanceAsync(_reconnectCts.Token);
            }
        };

        Client.Disconnected += () =>
        {
            Log($"Disconnected event received. manualDisconnect={_manualDisconnect}");
            IsConnected.Value = false;
            RootNode.Value = null;
            OverlayNodes.Value = new List<ElementNode>();
            SelectedElementId.Value = null;
            Properties.Value = new List<PropertyInfo>();
            ExpandedStates.Clear();

            if (_manualDisconnect)
            {
                Log("Manual disconnect — stopping, clearing logs.");
                ConnectionStatus.Value = "Disconnected";
                Logs.Value = new List<LogMessage>();
            }
            else
            {
                Log("Unexpected disconnect — starting auto-reconnect.");
                ConnectionStatus.Value = "Disconnected — reconnecting...";
                _ = AutoReconnectAsync();
            }
        };

        Client.MessageReceived += (msg) =>
        {
            if (msg is TreeChangedEvent)
            {
                Log("Received TreeChangedEvent — refreshing tree.");
                ConnectionStatus.Value = "Tree changed - refreshing...";
                SelectedElementId.Value = null;
                Properties.Value = new List<PropertyInfo>();
                ExpandedStates.Clear();
                _ = RefreshTreeAsync();
            }
            else if (msg is OverlaysChangedEvent)
            {
                ConnectionStatus.Value = "Overlays changed - refreshing...";
                _ = RefreshOverlaysAsync();
            }
            else if (msg is PropertiesResponse propertiesResponse)
            {
                if (SelectedElementId.Value == propertiesResponse.ElementId)
                {
                    Properties.Value = propertiesResponse.Properties ?? new List<PropertyInfo>();
                }
            }
            else if (msg is LogMessage logMsg)
            {
                var list = Logs.Value;
                list.Add(logMsg);
                Logs.Value = new List<LogMessage>(list);
            }
        };

        IsHighlightEnabled.Subscribe(enabled =>
        {
            if (SelectedElementId.Value != null)
            {
                _ = Client.HighlightAsync(enabled ? SelectedElementId.Value : null);
            }
        });

        IsDirtyHeatmapEnabled.Subscribe(enabled =>
        {
            if (Client.IsConnected)
                _ = Client.SetDirtyHeatmapAsync(enabled);
        });

        IsOverdrawEnabled.Subscribe(enabled =>
        {
            if (Client.IsConnected)
                _ = Client.SetOverdrawVisualizerAsync(enabled);
        });

        IsExtendedStatsEnabled.Subscribe(enabled =>
        {
            if (Client.IsConnected)
                _ = Client.SetExtendedStatsAsync(enabled);
        });

        IsPerformancePanelOpen.Subscribe(open =>
        {
            if (open && Client.IsConnected)
            {
                IsExtendedStatsEnabled.Value = true;
                _ = PollPerformanceAsync(_reconnectCts.Token);
            }
            else if (!open && Client.IsConnected)
            {
                IsExtendedStatsEnabled.Value = false;
            }
        });
    }

    private async Task PollPerformanceAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && IsPerformancePanelOpen.Value && Client.IsConnected)
        {
            var stats = await Client.GetPerformanceStatsAsync();
            if (stats != null)
            {
                PerformanceStats.Value = stats;
                DirtyLog.Value = stats.DirtyLog ?? new List<DirtyEntryDto>();
            }
            try { await Task.Delay(500, token); }
            catch (TaskCanceledException) { return; }
        }
    }

    public async Task RefreshTreeAsync()
    {
        if (!Client.IsConnected)
        {
            Log("RefreshTreeAsync — skipped, not connected.");
            return;
        }

        Log("RefreshTreeAsync — starting (up to 6 attempts).");
        TreeResponse? response = null;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(500);
                if (!Client.IsConnected)
                {
                    Log($"RefreshTreeAsync — disconnected during retry (attempt {attempt + 1}).");
                    return;
                }
            }

            response = await Client.GetTreeAsync();
            Log($"RefreshTreeAsync — attempt {attempt + 1}: root={response?.Root?.TypeName ?? "null"}, children={response?.Root?.Children.Count ?? -1}");

            if (response?.Root != null && response.Root.Children.Count > 0)
                break;
        }

        RootNode.Value = response?.Root;
        OverlayNodes.Value = response?.Overlays ?? new List<ElementNode>();
        ConnectionStatus.Value = $"Connected to {Host.Value}:{Port.Value}";
        Log($"RefreshTreeAsync — done. RootNode={RootNode.Value?.TypeName ?? "null"}");
    }

    public async Task RefreshOverlaysAsync()
    {
        if (!Client.IsConnected) return;

        var response = await Client.GetOverlaysAsync();
        OverlayNodes.Value = response?.Overlays ?? new List<ElementNode>();
        ConnectionStatus.Value = $"Connected to {Host.Value}:{Port.Value}";
    }

    public async Task LoadPropertiesAsync(string elementId)
    {
        if (!Client.IsConnected) return;

        await Client.SendAsync(new GetPropertiesRequest { ElementId = elementId });
    }

    /// <summary>
    /// Connects intentionally (user action). Cancels any running auto-reconnect first.
    /// </summary>
    public async Task<bool> ConnectManuallyAsync()
    {
        Log("ConnectManuallyAsync — cancelling auto-reconnect, connecting...");
        _reconnectCts.Cancel();
        _reconnectCts = new CancellationTokenSource(); // fresh token so polling works after connect

        ConnectionStatus.Value = "Connecting...";
        var success = await Client.ConnectAsync(Host.Value, Port.Value);
        if (!success)
        {
            ConnectionStatus.Value = "Connection failed";
            Log("ConnectManuallyAsync — failed.");
        }
        else
        {
            Log("ConnectManuallyAsync — succeeded.");
        }
        return success;
    }

    /// <summary>
    /// Disconnects intentionally (user action). Suppresses auto-reconnect.
    /// </summary>
    public async Task DisconnectManuallyAsync()
    {
        Log("DisconnectManuallyAsync — cancelling reconnect and disconnecting.");
        _manualDisconnect = true;
        _reconnectCts.Cancel();
        await Client.DisconnectAsync();
    }

    /// <summary>
    /// Attempts to reconnect after an unexpected disconnect.
    /// Retries indefinitely: every 1 s for the first 30 attempts, then every 3 s.
    /// </summary>
    private async Task AutoReconnectAsync()
    {
        _reconnectCts.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var token = _reconnectCts.Token;

        Log("AutoReconnectAsync — starting loop.");
        int attempt = 0;
        while (!token.IsCancellationRequested)
        {
            attempt++;
            int delayMs = attempt <= 30 ? 1000 : 3000;

            try { await Task.Delay(delayMs, token); }
            catch (TaskCanceledException)
            {
                Log("AutoReconnectAsync — cancelled during delay.");
                return;
            }

            if (token.IsCancellationRequested)
            {
                Log("AutoReconnectAsync — cancelled before attempt.");
                return;
            }

            Log($"AutoReconnectAsync — attempt {attempt} ({Host.Value}:{Port.Value})...");
            ConnectionStatus.Value = $"Reconnecting... (attempt {attempt})";

            var success = await Client.ConnectAsync(Host.Value, Port.Value);
            if (success)
            {
                Log($"AutoReconnectAsync — reconnected on attempt {attempt}!");
                return;
            }
        }

        Log("AutoReconnectAsync — loop ended (cancelled).");
    }

    public void Dispose()
    {
        _reconnectCts.Cancel();
        _reconnectCts.Dispose();
        Client.Dispose();
    }
}
