using System.Net.Sockets;
using System.Text;
using Rayo.DevTool.Shared.Protocol;

namespace Rayo.DevTool;

/// <summary>
/// Client that connects to a Rayo application's DevTool agent
/// </summary>
public class DevToolClient : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private readonly StringBuilder _messageBuffer = new();

    // Prevents concurrent ConnectAsync / Disconnect calls from racing.
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public event Action<DevToolMessage>? MessageReceived;
    public event Action? Connected;
    public event Action? Disconnected;

    public bool IsConnected => _client?.Connected == true;

    private static string Ts => DateTime.Now.ToString("HH:mm:ss.fff");

    public async Task<bool> ConnectAsync(string host, int port)
    {
        Log($"ConnectAsync({host}:{port}) — waiting for lock...");
        await _connectLock.WaitAsync();
        try
        {
            Log($"ConnectAsync({host}:{port}) — lock acquired, closing previous connection...");
            // Close any previous connection so the server's HandleClientAsync exits
            // and can accept this new connection.
            await CloseConnectionAsync();

            Log($"ConnectAsync({host}:{port}) — creating TcpClient...");
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            _messageBuffer.Clear();

            Log($"ConnectAsync({host}:{port}) — TCP connected, starting read loop.");
            _readTask = Task.Run(ReadMessagesAsync);
            Log($"ConnectAsync({host}:{port}) — firing Connected event.");
            Connected?.Invoke();

            return true;
        }
        catch (Exception ex)
        {
            Log($"ConnectAsync({host}:{port}) — FAILED: {ex.Message}");
            await CloseConnectionAsync();
            return false;
        }
        finally
        {
            _connectLock.Release();
            Log($"ConnectAsync({host}:{port}) — lock released.");
        }
    }

    public async Task DisconnectAsync()
    {
        Log("DisconnectAsync — waiting for lock...");
        await _connectLock.WaitAsync();
        try
        {
            await CloseConnectionAsync();
        }
        finally
        {
            _connectLock.Release();
        }
        Log("DisconnectAsync — firing Disconnected event.");
        NotifyDisconnected();
    }

    /// <summary>
    /// Closes the TCP socket, cancels the read loop, and waits for the read task
    /// to finish so there is no overlap between old and new connections.
    /// Must be called while holding <see cref="_connectLock"/>.
    /// </summary>
    private async Task CloseConnectionAsync()
    {
        _cts?.Cancel();
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        // Wait for the read task to actually finish so there is no concurrent access
        // to shared state (_messageBuffer, events) from an old connection.
        if (_readTask != null)
        {
            Log("CloseConnectionAsync — waiting for read task to finish...");
            try { await _readTask.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch { Log("CloseConnectionAsync — read task wait timed out or faulted."); }
            _readTask = null;
        }

        _client = null;
        _stream = null;
        _cts?.Dispose();
        _cts = null;
        Log("CloseConnectionAsync — done.");
    }

    private async Task ReadMessagesAsync()
    {
        Log("ReadMessagesAsync — started.");
        var buffer = new byte[8192];
        var token = _cts!.Token;

        while (_client?.Connected == true && !token.IsCancellationRequested)
        {
            try
            {
                var bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0)
                {
                    Log("ReadMessagesAsync — server closed the connection gracefully (0 bytes).");
                    break;
                }

                _messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                var content = _messageBuffer.ToString();
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        try
                        {
                            var message = MessageSerializer.Deserialize(lines[i].Trim());
                            if (message != null)
                            {
                                MessageReceived?.Invoke(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"ReadMessagesAsync — error processing message: {ex.Message}");
                        }
                    }
                }

                _messageBuffer.Clear();
                _messageBuffer.Append(lines[^1]);
            }
            catch (OperationCanceledException)
            {
                // Intentional cancel from CloseConnectionAsync — exit silently.
                Log("ReadMessagesAsync — cancelled (intentional), exiting.");
                return;
            }
            catch (Exception ex)
            {
                Log($"ReadMessagesAsync — read error: {ex.GetType().Name}: {ex.Message}");
                break;
            }
        }

        // If we reach here the connection was lost (not intentionally cancelled).
        // Close the socket so the server detects the disconnect immediately (TCP RST)
        // instead of keeping a half-open connection.
        Log($"ReadMessagesAsync — loop ended. Connected={_client?.Connected}, Cancelled={token.IsCancellationRequested}");
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        Log("ReadMessagesAsync — firing Disconnected.");
        NotifyDisconnected();
    }

    private void NotifyDisconnected()
    {
        var handler = Disconnected;
        if (handler == null) return;

        _ = Task.Run(handler);
    }

    public async Task SendAsync(DevToolMessage message)
    {
        var stream = _stream;
        if (stream == null || !IsConnected) return;

        try
        {
            var json = MessageSerializer.Serialize(message) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
        }
        catch (Exception)
        {
            // Stream broken — the read loop will detect it and fire Disconnected.
        }
    }

    public async Task<TreeResponse?> GetTreeAsync()
    {
        var tcs = new TaskCompletionSource<TreeResponse?>();

        void Handler(DevToolMessage msg)
        {
            if (msg is TreeResponse response)
            {
                MessageReceived -= Handler;
                tcs.TrySetResult(response);
            }
        }

        MessageReceived += Handler;
        await SendAsync(new GetTreeRequest());

        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Log("GetTreeAsync — TIMEOUT waiting for TreeResponse.");
            MessageReceived -= Handler;
            return null;
        }

        return await tcs.Task;
    }

    public async Task<OverlaysResponse?> GetOverlaysAsync()
    {
        var tcs = new TaskCompletionSource<OverlaysResponse?>();

        void Handler(DevToolMessage msg)
        {
            if (msg is OverlaysResponse response)
            {
                MessageReceived -= Handler;
                tcs.TrySetResult(response);
            }
        }

        MessageReceived += Handler;
        await SendAsync(new GetOverlaysRequest());

        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            MessageReceived -= Handler;
            return null;
        }

        return await tcs.Task;
    }

    public async Task<PropertiesResponse?> GetPropertiesAsync(string elementId)
    {
        var tcs = new TaskCompletionSource<PropertiesResponse?>();

        void Handler(DevToolMessage msg)
        {
            if (msg is PropertiesResponse response && response.ElementId == elementId)
            {
                MessageReceived -= Handler;
                tcs.TrySetResult(response);
            }
        }

        MessageReceived += Handler;
        await SendAsync(new GetPropertiesRequest { ElementId = elementId });

        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            MessageReceived -= Handler;
            return null;
        }

        return await tcs.Task;
    }

    public async Task HighlightAsync(string? elementId)
    {
        await SendAsync(new HighlightElementRequest { ElementId = elementId });
    }

    public async Task<PerformanceStatsResponse?> GetPerformanceStatsAsync()
    {
        var tcs = new TaskCompletionSource<PerformanceStatsResponse?>();

        void Handler(DevToolMessage msg)
        {
            if (msg is PerformanceStatsResponse r)
            {
                MessageReceived -= Handler;
                tcs.TrySetResult(r);
            }
        }

        MessageReceived += Handler;
        await SendAsync(new GetPerformanceStatsRequest());

        var timeout = Task.Delay(5000);
        if (await Task.WhenAny(tcs.Task, timeout) == timeout)
        {
            MessageReceived -= Handler;
            return null;
        }
        return await tcs.Task;
    }

    public async Task SetDirtyHeatmapAsync(bool enabled) =>
        await SendAsync(new SetDirtyHeatmapRequest { Enabled = enabled });

    public async Task SetOverdrawVisualizerAsync(bool enabled) =>
        await SendAsync(new SetOverdrawVisualizerRequest { Enabled = enabled });

    public async Task SetExtendedStatsAsync(bool enabled) =>
        await SendAsync(new SetExtendedStatsRequest { Enabled = enabled });

    public async Task ClearDirtyLogAsync() =>
        await SendAsync(new ClearDirtyLogRequest());

    public async Task<bool> SetPropertyAsync(string elementId, string propertyName, object value)
    {
        var tcs = new TaskCompletionSource<bool>();
        var requestId = "";

        void Handler(DevToolMessage msg)
        {
            if (msg is ResultResponse response && response.RequestId == requestId)
            {
                MessageReceived -= Handler;
                tcs.TrySetResult(response.Success);
            }
        }

        var request = new SetPropertyRequest
        {
            ElementId = elementId,
            PropertyName = propertyName,
            Value = System.Text.Json.JsonSerializer.SerializeToElement(value)
        };
        requestId = request.Id;

        MessageReceived += Handler;
        await SendAsync(request);

        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            MessageReceived -= Handler;
            return false;
        }

        return await tcs.Task;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        _cts?.Dispose();
        _connectLock.Dispose();
    }

    private static void Log(string msg) =>
        Console.WriteLine($"[DevTool Client {Ts}] {msg}");
}
