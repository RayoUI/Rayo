using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using ProtocolPropertyInfo = Rayo.DevTool.Shared.Protocol.PropertyInfo;

namespace Rayo.DevTools;

/// <summary>
/// Agent that runs inside a Rayo application to enable DevTool inspection
/// </summary>
public class DevToolAgent : IDisposable
{
    private readonly UITree _uiTree;
    private readonly IRenderer _renderer;
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly int _port;
    private readonly Dictionary<string, WeakReference<VisualElement>> _elementCache = new();
    private string? _highlightedElementId;
    private bool _isRunning;
    private DateTime _lastTreeStructureNotification = DateTime.MinValue;
    private readonly TimeSpan _treeChangeDebounceInterval = TimeSpan.FromMilliseconds(500);
    // Serializes all stream writes — NetworkStream does not support concurrent WriteAsync calls.
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

    private static string Ts => DateTime.Now.ToString("HH:mm:ss.fff");
    private static void Log(string msg) => Console.WriteLine($"[DevTool Agent {Ts}] {msg}");

    public bool IsConnected => _client?.Connected == true;
    public int Port => _port;

    public DevToolAgent(UITree uiTree, IRenderer renderer, int port = 9999)
    {
        _uiTree = uiTree;
        _renderer = renderer;
        _port = port;

        // Subscribe to tree changes to notify connected clients
        _uiTree.RootChanged += OnTreeChanged;

        // Subscribe to overlay changes
        _uiTree.OverlaysChanged += OnOverlaysChanged;

        // Subscribe to structural changes (add/remove children)
        VisualElement.TreeStructureChanged += OnTreeStructureChanged;

        // Subscribe to logs
        DevToolLogger.OnLog += SendLogMessage;
    }

    public void Start()
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        // Use IPAddress.Any to accept connections from any network interface (not just localhost)
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;

        _listenTask = Task.Run(AcceptClientsAsync, _cts.Token);
        Log($"Agent listening on 0.0.0.0:{_port}");
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _client?.Close();
        _listener?.Stop();
        _cts?.Dispose();
    }

    private async Task AcceptClientsAsync()
    {
        Log("AcceptClientsAsync — waiting for connections...");
        while (_isRunning && !_cts!.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync();
                var remoteEndPoint = client.Client.RemoteEndPoint;

                // Close previous client if still lingering so resources are freed
                // and IsConnected reflects the new connection immediately.
                CloseCurrentClient();

                _client = client;
                _stream = _client.GetStream();
                Log($"Client connected from {remoteEndPoint}.");

                await HandleClientAsync();

                // Client disconnected — clean up so AcceptTcpClientAsync can
                // accept the next connection without leaving a half-open socket.
                Log("HandleClientAsync returned — cleaning up, waiting for next connection.");
                CloseCurrentClient();
            }
            catch (ObjectDisposedException)
            {
                Log("AcceptClientsAsync — listener disposed, stopping.");
                break;
            }
            catch (Exception ex)
            {
                Log($"AcceptClientsAsync — error: {ex.GetType().Name}: {ex.Message}");
            }
        }
        Log("AcceptClientsAsync — loop ended.");
    }

    /// <summary>
    /// Closes and nulls the current client/stream so <see cref="IsConnected"/> returns false
    /// and any pending <see cref="SendMessageAsync"/> calls fail fast.
    /// </summary>
    private void CloseCurrentClient()
    {
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        _stream = null;
        _client = null;
    }

    private async Task HandleClientAsync()
    {
        var buffer = new byte[8192];
        var messageBuffer = new StringBuilder();

        Log("HandleClientAsync — read loop started.");
        while (_client?.Connected == true && !_cts!.Token.IsCancellationRequested)
        {
            try
            {
                var bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                if (bytesRead == 0)
                {
                    Log("HandleClientAsync — 0 bytes read (graceful close by client).");
                    break;
                }

                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                // Process complete messages (newline-delimited)
                var content = messageBuffer.ToString();
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        await ProcessMessageAsync(lines[i].Trim());
                    }
                }

                // Keep incomplete message in buffer
                messageBuffer.Clear();
                messageBuffer.Append(lines[^1]);
            }
            catch (OperationCanceledException)
            {
                Log("HandleClientAsync — cancelled (server shutting down).");
                break;
            }
            catch (IOException ex)
            {
                Log($"HandleClientAsync — IOException (client disconnected): {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Log($"HandleClientAsync — unexpected error: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Log($"HandleClientAsync — loop ended. Connected={_client?.Connected}, Cancelled={_cts?.Token.IsCancellationRequested}");
    }

    private async Task ProcessMessageAsync(string json)
    {
        try
        {
            var message = MessageSerializer.Deserialize(json);
            if (message == null) return;

            DevToolMessage? response = message switch
            {
                GetTreeRequest => HandleGetTree(),
                GetOverlaysRequest => HandleGetOverlays(),
                GetPropertiesRequest req => HandleGetProperties(req),
                SetPropertyRequest req => HandleSetProperty(req),
                HighlightElementRequest req => HandleHighlight(req),
                GetPerformanceStatsRequest => HandleGetPerformanceStats(),
                SetDirtyHeatmapRequest req => HandleSetDirtyHeatmap(req),
                SetOverdrawVisualizerRequest req => HandleSetOverdraw(req),
                SetExtendedStatsRequest req => HandleSetExtendedStats(req),
                ClearDirtyLogRequest => HandleClearDirtyLog(),
                _ => null
            };

            if (response != null)
            {
                await SendMessageAsync(response);
            }
        }
        catch (Exception ex)
        {
            Log($"ProcessMessageAsync — error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task SendMessageAsync(DevToolMessage message)
    {
        if (_stream == null || !_client!.Connected) return;

        var json = MessageSerializer.Serialize(message) + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync();
        try
        {
            if (_stream == null || !_client.Connected) return;
            await _stream.WriteAsync(bytes, 0, bytes.Length);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            Log($"SendMessageAsync — send failed ({message.GetType().Name}): {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private TreeResponse HandleGetTree()
    {
        _elementCache.Clear();
        var root = _uiTree.Root;

        var response = new TreeResponse
        {
            Root = root != null ? BuildElementNode(root) : null
        };

        // Include overlays (Drawer, Dialog, Menu, etc.)
        foreach (var overlay in _uiTree.Overlays)
        {
            response.Overlays.Add(BuildElementNode(overlay));
        }

        return response;
    }

    private OverlaysResponse HandleGetOverlays()
    {
        // Don't clear element cache - overlays might reference elements we already know
        var response = new OverlaysResponse();

        foreach (var overlay in _uiTree.Overlays)
        {
            response.Overlays.Add(BuildElementNode(overlay));
        }

        return response;
    }

    private ElementNode BuildElementNode(VisualElement element)
    {
        var id = GetOrCreateElementId(element);
        var childrenArray = element.GetChildren().ToArray();

        var node = new ElementNode
        {
            Id = id,
            TypeName = element.GetType().Name,
            Name = element.Id,
            X = element.ComputedX,
            Y = element.ComputedY,
            Width = element.ComputedWidth,
            Height = element.ComputedHeight,
            IsVisible = element.IsVisible,
            ChildCount = childrenArray.Length
        };

        foreach (var child in childrenArray)
        {
            node.Children.Add(BuildElementNode(child));
        }

        return node;
    }

    private string GetOrCreateElementId(VisualElement element)
    {
        // Check if already cached
        foreach (var kvp in _elementCache)
        {
            if (kvp.Value.TryGetTarget(out var cached) && ReferenceEquals(cached, element))
            {
                return kvp.Key;
            }
        }

        var id = Guid.NewGuid().ToString("N")[..12];
        _elementCache[id] = new WeakReference<VisualElement>(element);
        return id;
    }

    private VisualElement? GetElementById(string id)
    {
        if (_elementCache.TryGetValue(id, out var weakRef))
        {
            if (weakRef.TryGetTarget(out var element))
            {
                return element;
            }
            _elementCache.Remove(id);
        }
        return null;
    }

    private PropertiesResponse HandleGetProperties(GetPropertiesRequest request)
    {
        var response = new PropertiesResponse { ElementId = request.ElementId };
        var element = GetElementById(request.ElementId);

        if (element == null) return response;

        var type = element.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && 
                        IsInspectableProperty(p) && 
                        !typeof(Delegate).IsAssignableFrom(p.PropertyType));

        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(element);
                var propInfo = CreatePropertyInfo(prop, value);
                if (propInfo != null)
                {
                    response.Properties.Add(propInfo);
                }
            }
            catch
            {
                // Skip properties that throw
            }
        }

        // Sort by category then name
        response.Properties = response.Properties
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToList();

        return response;
    }

    private bool IsInspectableProperty(System.Reflection.PropertyInfo prop)
    {
        var type = prop.PropertyType;

        // Skip complex types and collections (for now)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            return false;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return false;
        if (prop.Name == "Children" || prop.Name == "Parent")
            return false;
        if (prop.Name is "ComputedX" or "ComputedY" or "ComputedWidth" or "ComputedHeight" or "DesiredWidth" or "DesiredHeight")
            return false;

        return true;
    }

    private ProtocolPropertyInfo? CreatePropertyInfo(System.Reflection.PropertyInfo prop, object? value)
    {
        var type = prop.PropertyType;
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        var info = new ProtocolPropertyInfo
        {
            Name = prop.Name,
            TypeName = type.Name,
            IsReadOnly = !prop.CanWrite,
            Category = GetPropertyCategory(prop.Name)
        };

        // Determine editor type and serialize value
        if (effectiveType == typeof(bool))
        {
            info.Editor = "boolean";
            info.Value = value;
        }
        else if (effectiveType == typeof(string))
        {
            info.Editor = "text";
            info.Value = value;
        }
        else if (IsNumericType(effectiveType))
        {
            info.Editor = "number";
            // Format special float values for better readability
            if (value is float f)
            {
                if (float.IsInfinity(f) || float.IsNaN(f))
                {
                    info.Value = null;
                    info.IsReadOnly = true;
                }
                else
                    info.Value = value;
            }
            else if (value is double d)
            {
                if (double.IsInfinity(d) || double.IsNaN(d))
                {
                    info.Value = null;
                    info.IsReadOnly = true;
                }
                else
                    info.Value = value;
            }
            else
            {
                info.Value = value;
            }
        }
        else if (effectiveType == typeof(Color))
        {
            info.Editor = "color";
            if (value is Color color)
            {
                info.Value = ToHexColor(color);
            }
        }
        else if (typeof(Brush).IsAssignableFrom(effectiveType))
        {
            info.Editor = "brushcolor";
            if (value is Brush brush)
            {
                info.Value = ToHexColor(brush.PrimaryColor);
            }
        }
        else if (effectiveType.IsEnum)
        {
            info.Editor = "enum";
            info.Value = value?.ToString();
            info.EnumValues = Enum.GetNames(effectiveType).ToList();
        }
        else if (effectiveType == typeof(Thickness))
        {
            info.Editor = "thickness";
            if (value is Thickness t)
            {
                info.Value = $"{t.Left},{t.Top},{t.Right},{t.Bottom}";
            }
        }
        else if (effectiveType == typeof(CornerRadius))
        {
            info.Editor = "cornerradius";
            if (value is CornerRadius cr)
            {
                info.Value = $"{cr.TopLeft},{cr.TopRight},{cr.BottomRight},{cr.BottomLeft}";
            }
        }
        else
        {
            // For other types, just show as read-only text
            info.Editor = "text";
            info.Value = value?.ToString() ?? "(null)";
            info.IsReadOnly = true;
        }

        return info;
    }

    private string GetPropertyCategory(string propertyName)
    {
        return propertyName switch
        {
            "Width" or "Height" or "MinWidth" or "MinHeight" or "MaxWidth" or "MaxHeight" => "Size",
            "ComputedX" or "ComputedY" or "ComputedWidth" or "ComputedHeight" => "Computed",
            "DesiredWidth" or "DesiredHeight" => "Computed",
            "Margin" or "Padding" => "Spacing",
            "HorizontalAlignment" or "VerticalAlignment" => "Alignment",
            "Background" or "Foreground" or "BorderColor" => "Appearance",
            "BorderWidth" or "BorderRadius" => "Border",
            "IsVisible" or "IsEnabled" or "IsFocused" => "State",
            "Name" or "Tag" => "Identity",
            _ => "General"
        };
    }

    private ResultResponse HandleSetProperty(SetPropertyRequest request)
    {
        var element = GetElementById(request.ElementId);
        if (element == null)
        {
            return new ResultResponse
            {
                Success = false,
                Error = "Element not found",
                RequestId = request.Id
            };
        }

        try
        {
            var prop = element.GetType().GetProperty(request.PropertyName);
            if (prop == null || !prop.CanWrite)
            {
                return new ResultResponse
                {
                    Success = false,
                    Error = "Property not found or read-only",
                    RequestId = request.Id
                };
            }

            var value = ConvertValue(request.Value, prop.PropertyType);
            value = SanitizePropertyValue(request.PropertyName, prop.PropertyType, value);
            prop.SetValue(element, value);

            // Persist this change in the style baseline so it survives subsequent style
            // reapplications triggered by class or orientation changes.
            element.UpdateStyleBaselineEntry(request.PropertyName, value);

            // When Classes changes, re-apply styles so class selectors take effect immediately.
            // Walk up the parent chain to find the owning UserControl and re-apply both its
            // global and component (BuildStyles) rules so class selectors are re-evaluated.
            if (request.PropertyName is nameof(VisualElement.Classes) or nameof(VisualElement.Id))
            {
                ReapplyStylesForTree();
            }

            // Force layout and render update after property change
            element.MarkNeedsLayout();
            element.MarkNeedsPaint();
            _uiTree.MarkNeedsLayout();
            _uiTree.MarkNeedsRender();

            return new ResultResponse
            {
                Success = true,
                RequestId = request.Id
            };
        }
        catch (Exception ex)
        {
            return new ResultResponse
            {
                Success = false,
                Error = ex.Message,
                RequestId = request.Id
            };
        }
    }

    private void ReapplyStylesForTree()
    {
        var globalStyles = UIApplication.Current?.GlobalStyles;
        if (globalStyles != null)
        {
            if (_uiTree.Root != null && _uiTree.Root is not UserControl)
                StyleEngine.Apply(globalStyles, _uiTree.Root);

            foreach (var overlay in _uiTree.Overlays)
            {
                if (overlay is not UserControl)
                    StyleEngine.Apply(globalStyles, overlay);
            }
        }

        if (_uiTree.Root != null)
            ReapplyStylesRecursive(_uiTree.Root);

        foreach (var overlay in _uiTree.Overlays)
            ReapplyStylesRecursive(overlay);
    }

    private static void ReapplyStylesRecursive(VisualElement element)
    {
        if (element is UserControl uc)
            uc.ReapplyStyles();

        foreach (var child in element.GetChildren())
            ReapplyStylesRecursive(child);
    }

    private object? ConvertValue(JsonElement jsonValue, Type targetType)
    {
        // Unwrap Nullable<T> → convert as T
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            if (jsonValue.ValueKind == System.Text.Json.JsonValueKind.Null)
                return null;
            return ConvertValue(jsonValue, underlying);
        }

        if (targetType == typeof(bool))
            return jsonValue.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(jsonValue.GetString(), out var parsed) => parsed,
                _ => throw new InvalidOperationException($"Cannot convert '{jsonValue}' to bool.")
            };
        if (targetType == typeof(string))
            return jsonValue.ValueKind == JsonValueKind.Null ? null : jsonValue.GetString() ?? jsonValue.ToString();
        if (IsNumericType(targetType))
            return ConvertNumericValue(jsonValue, targetType);
        if (targetType == typeof(Color))
        {
            return ParseColorValue(jsonValue);
        }
        if (typeof(Brush).IsAssignableFrom(targetType))
        {
            return new SolidColorBrush(ParseColorValue(jsonValue));
        }
        if (targetType.IsEnum)
        {
            if (jsonValue.ValueKind == JsonValueKind.String)
            {
                var str = jsonValue.GetString();
                return Enum.Parse(targetType, str!, ignoreCase: true);
            }

            var enumNumber = ConvertNumericValue(jsonValue, Enum.GetUnderlyingType(targetType));
            return Enum.ToObject(targetType, enumNumber!);
        }
        if (targetType == typeof(Thickness))
        {
            var parts = ParseFloatQuad(jsonValue, "Thickness");
            return new Thickness(parts[0], parts[1], parts[2], parts[3]);
        }
        if (targetType == typeof(CornerRadius))
        {
            var parts = ParseFloatQuad(jsonValue, "CornerRadius");
            return new CornerRadius(parts[0], parts[1], parts[2], parts[3]);
        }

        return null;
    }

    private static object? SanitizePropertyValue(string propertyName, Type propertyType, object? value)
    {
        if (value == null)
            return null;

        var effectiveType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (effectiveType == typeof(Thickness) && value is Thickness thickness)
        {
            return new Thickness(
                Math.Max(0f, thickness.Left),
                Math.Max(0f, thickness.Top),
                Math.Max(0f, thickness.Right),
                Math.Max(0f, thickness.Bottom));
        }

        if (effectiveType == typeof(CornerRadius) && value is CornerRadius cornerRadius)
        {
            return new CornerRadius(
                Math.Max(0f, cornerRadius.TopLeft),
                Math.Max(0f, cornerRadius.TopRight),
                Math.Max(0f, cornerRadius.BottomRight),
                Math.Max(0f, cornerRadius.BottomLeft));
        }

        if (IsNumericType(effectiveType))
        {
            var bounds = GetNumericBounds(propertyName);
            if (bounds.MinValue.HasValue || bounds.MaxValue.HasValue)
                return ClampNumericValue(value, effectiveType, bounds.MinValue, bounds.MaxValue);
        }

        return value;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private static (double? MinValue, double? MaxValue) GetNumericBounds(string propertyName)
    {
        return propertyName switch
        {
            "Opacity" => (0d, 1d),
            "Scale" => (0d, null),
            "Width" or "Height" or "MinWidth" or "MinHeight" or "MaxWidth" or "MaxHeight" or
            "BorderWidth" or "StrokeWidth" or "StrokeThickness" or
            "FontSize" or "Spacing" or "ItemSpacing" or "RowSpacing" or "ColumnSpacing" or
            "Gap" or "RowGap" or
            "Radius" or "RadiusX" or "RadiusY" or "InnerRadius" or
            "BlurRadius" or "ContactRadius" or "BadgeSize" or "IconSize" or
            "ItemHeight" or "RowHeight" or "HeaderHeight" or "DrawerHeight" or "BarHeight" or
            "TrackHeight" or "TrackWidth" or "SplitterSize" or "ThumbSize" or "CircleSize" or
            "ButtonWidth" or "TabWidth" or "VerticalTabWidth" or "TabHeight" or "VerticalTabHeight" or
            "LineHeight" or "MaxDropdownHeight" or "IndentSize" or "AnimationDuration" or "TransitionDuration" or
            "CornerRadius" or "BorderRadius"
                => (0d, null),
            _ => (null, null)
        };
    }

    private static object ClampNumericValue(object value, Type targetType, double? minValue, double? maxValue)
    {
        double numericValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        if (minValue.HasValue && numericValue < minValue.Value)
            numericValue = minValue.Value;
        if (maxValue.HasValue && numericValue > maxValue.Value)
            numericValue = maxValue.Value;

        return targetType == typeof(byte) ? (byte)numericValue :
               targetType == typeof(sbyte) ? (sbyte)numericValue :
               targetType == typeof(short) ? (short)numericValue :
               targetType == typeof(ushort) ? (ushort)numericValue :
               targetType == typeof(int) ? (int)numericValue :
               targetType == typeof(uint) ? (uint)numericValue :
               targetType == typeof(long) ? (long)numericValue :
               targetType == typeof(ulong) ? (ulong)numericValue :
               targetType == typeof(float) ? (float)numericValue :
               targetType == typeof(double) ? numericValue :
               targetType == typeof(decimal) ? (decimal)numericValue :
               value;
    }

    private static object ConvertNumericValue(JsonElement jsonValue, Type targetType)
    {
        if (jsonValue.ValueKind != JsonValueKind.Number && jsonValue.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Cannot convert '{jsonValue}' to {targetType.Name}.");

        string text = jsonValue.ValueKind == JsonValueKind.String
            ? jsonValue.GetString() ?? throw new InvalidOperationException($"Cannot convert null to {targetType.Name}.")
            : jsonValue.GetRawText();

        return targetType == typeof(byte) ? byte.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(sbyte) ? sbyte.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(short) ? short.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(ushort) ? ushort.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(int) ? int.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(uint) ? uint.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(long) ? long.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(ulong) ? ulong.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(float) ? float.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(double) ? double.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               targetType == typeof(decimal) ? decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture) :
               throw new InvalidOperationException($"Unsupported numeric type {targetType.Name}.");
    }

    private static float[] ParseFloatQuad(JsonElement jsonValue, string typeName)
    {
        if (jsonValue.ValueKind == JsonValueKind.String)
        {
            var str = jsonValue.GetString();
            var parts = str!.Split(',').Select(part => float.Parse(part, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            if (parts.Length == 4)
                return parts;
        }
        else if (jsonValue.ValueKind == JsonValueKind.Object)
        {
            if (typeName == "Thickness")
            {
                return new[]
                {
                    GetRequiredFloatProperty(jsonValue, "Left"),
                    GetRequiredFloatProperty(jsonValue, "Top"),
                    GetRequiredFloatProperty(jsonValue, "Right"),
                    GetRequiredFloatProperty(jsonValue, "Bottom")
                };
            }

            return new[]
            {
                GetRequiredFloatProperty(jsonValue, "TopLeft"),
                GetRequiredFloatProperty(jsonValue, "TopRight"),
                GetRequiredFloatProperty(jsonValue, "BottomRight"),
                GetRequiredFloatProperty(jsonValue, "BottomLeft")
            };
        }

        throw new InvalidOperationException($"Cannot convert '{jsonValue}' to {typeName}.");
    }

    private static float GetRequiredFloatProperty(JsonElement jsonValue, string propertyName)
    {
        if (!jsonValue.TryGetProperty(propertyName, out var propertyValue))
            throw new InvalidOperationException($"Missing '{propertyName}' component.");

        return propertyValue.ValueKind == JsonValueKind.Number
            ? propertyValue.GetSingle()
            : float.Parse(propertyValue.GetString() ?? throw new InvalidOperationException($"Missing '{propertyName}' value."),
                System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Color ParseColorValue(JsonElement jsonValue)
    {
        if (jsonValue.ValueKind == JsonValueKind.String)
            return ParseColor(jsonValue.GetString());

        if (jsonValue.ValueKind == JsonValueKind.Object)
        {
            if (jsonValue.TryGetProperty("R", out var r) &&
                jsonValue.TryGetProperty("G", out var g) &&
                jsonValue.TryGetProperty("B", out var b))
            {
                var alpha = jsonValue.TryGetProperty("A", out var a) ? a.GetSingle() : 1f;
                return new Color(r.GetSingle(), g.GetSingle(), b.GetSingle(), alpha);
            }

            if (jsonValue.TryGetProperty("PrimaryColor", out var primaryColor))
                return ParseColorValue(primaryColor);
        }

        throw new InvalidOperationException($"Cannot convert '{jsonValue}' to Color.");
    }

    private static string ToHexColor(Color color)
    {
        return $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}{(int)(color.A * 255):X2}";
    }

    private static Color ParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.Transparent;
        hex = hex.TrimStart('#');

        if (hex.Length == 6)
        {
            return new Color(
                Convert.ToInt32(hex[0..2], 16),
                Convert.ToInt32(hex[2..4], 16),
                Convert.ToInt32(hex[4..6], 16)
            );
        }
        if (hex.Length == 8)
        {
            return new Color(
                Convert.ToInt32(hex[0..2], 16),
                Convert.ToInt32(hex[2..4], 16),
                Convert.ToInt32(hex[4..6], 16),
                Convert.ToInt32(hex[6..8], 16)
            );
        }

        return Color.Transparent;
    }

    private ResultResponse HandleHighlight(HighlightElementRequest request)
    {
        _highlightedElementId = request.ElementId;
        _uiTree.MarkNeedsRender(); // Force repaint

        return new ResultResponse
        {
            Success = true,
            RequestId = request.Id
        };
    }

    private PerformanceStatsResponse HandleGetPerformanceStats()
    {
        var latest = PerformanceTracker.LatestFrame;
        var history = PerformanceTracker.GetFrameHistory();
        var dirtyLog = PerformanceTracker.GetDirtyLog(150);

        var frames = new System.Collections.Generic.List<FrameStatsDto>(history.Length);
        foreach (var f in history)
        {
            frames.Add(new FrameStatsDto
            {
                Fps     = f.FpsSnapshot,
                FrameMs = f.FrameTimeMs,
                MeasMs  = f.MeasureTimeMs,
                RendMs  = f.RenderTimeMs,
                EvtMs   = f.EventTimeMs,
                LDirty  = f.LayoutDirtyCount,
                PDirty  = f.PaintDirtyCount,
                Elems   = f.ElementsRendered,
            });
        }

        var log = new System.Collections.Generic.List<DirtyEntryDto>(dirtyLog.Length);
        foreach (var e in dirtyLog)
        {
            log.Add(new DirtyEntryDto
            {
                ElementType = e.ElementType,
                ElementId   = e.ElementId,
                Classes     = e.Classes,
                IsLayout    = e.IsLayout,
                Timestamp   = e.Timestamp,
            });
        }

        return new PerformanceStatsResponse
        {
            Fps           = latest.FpsSnapshot,
            FrameTimeMs   = latest.FrameTimeMs,
            MeasureTimeMs = latest.MeasureTimeMs,
            RenderTimeMs  = latest.RenderTimeMs,
            EventTimeMs   = latest.EventTimeMs,
            ElemRendered  = latest.ElementsRendered,
            ElemMeasured  = latest.ElementsMeasured,
            LayoutDirty   = latest.LayoutDirtyCount,
            PaintDirty    = latest.PaintDirtyCount,
            Frames        = frames,
            DirtyLog      = log,
            HeatmapEnabled  = DirtyHeatmap.Enabled,
            OverdrawEnabled = OverdrawVisualizer.Enabled,
            ExtStatsEnabled = FpsOverlay.ShowExtendedStats,
        };
    }

    private ResultResponse HandleSetDirtyHeatmap(SetDirtyHeatmapRequest request)
    {
        DevToolExtensions.SetDirtyHeatmap(request.Enabled);
        return new ResultResponse { Success = true, RequestId = request.Id };
    }

    private ResultResponse HandleSetOverdraw(SetOverdrawVisualizerRequest request)
    {
        DevToolExtensions.SetOverdrawVisualizer(request.Enabled);
        return new ResultResponse { Success = true, RequestId = request.Id };
    }

    private ResultResponse HandleSetExtendedStats(SetExtendedStatsRequest request)
    {
        DevToolExtensions.SetExtendedStats(request.Enabled);
        return new ResultResponse { Success = true, RequestId = request.Id };
    }

    private ResultResponse HandleClearDirtyLog()
    {
        PerformanceTracker.ClearDirtyLog();
        return new ResultResponse { Success = true, RequestId = "" };
    }

    /// <summary>
    /// Called during rendering to draw highlight overlay
    /// </summary>
    public void RenderHighlight(IRenderer renderer)
    {
        if (string.IsNullOrEmpty(_highlightedElementId)) return;

        var element = GetElementById(_highlightedElementId);
        if (element == null || !IsElementReachable(element))
        {
            _highlightedElementId = null;
            return;
        }

        // Element is hidden — preserve selection so highlight resumes when it becomes visible again
        if (!element.IsVisible) return;

        // Skip highlighting if element has invalid dimensions (including infinity)
        // Allow very small dimensions (>0) but skip if exactly 0 or invalid
        if (element.ComputedWidth < 0.01f || element.ComputedHeight < 0.01f ||
            float.IsInfinity(element.ComputedWidth) || float.IsInfinity(element.ComputedHeight) ||
            float.IsNaN(element.ComputedWidth) || float.IsNaN(element.ComputedHeight))
        {
            return;
        }

        // Skip if position is invalid
        if (float.IsInfinity(element.ComputedX) || float.IsInfinity(element.ComputedY) ||
            float.IsNaN(element.ComputedX) || float.IsNaN(element.ComputedY))
        {
            return;
        }

        // Draw highlight overlay
        var color = new Color(59, 130, 246, 0.3f); // Semi-transparent blue
        var borderColor = new Color(59, 130, 246, 1f); // Solid blue

        var worldTransform = element.GetWorldRenderTransform();
        renderer.PushTransform(worldTransform);
        try
        {
            renderer.DrawRect(element.ComputedX, element.ComputedY,
                element.ComputedWidth, element.ComputedHeight, color);
            renderer.DrawRectOutline(element.ComputedX, element.ComputedY,
                element.ComputedWidth, element.ComputedHeight, 2, borderColor);
        }
        finally
        {
            renderer.PopTransform();
        }
    }

    /// <summary>
    /// Called when the UI tree root changes (e.g., hot reload).
    /// Notifies connected DevTool clients to refresh the main tree.
    /// A short delay is applied so that UserControl.EnsureBuilt() has time to
    /// run on the next layout frame before DevTool requests the tree.
    /// </summary>
    private void OnTreeChanged()
    {
        // Do NOT clear _elementCache here — HandleGetTree() already clears it and
        // Dictionary is not thread-safe for concurrent Clear()+read from the TCP thread.
        _highlightedElementId = null;

        // Suppress TreeStructureChanged notifications for the next debounce window.
        // This prevents premature GetTree requests before EnsureBuilt() has run —
        // the only relevant notification will be the delayed TreeChangedEvent below.
        _lastTreeStructureNotification = DateTime.UtcNow;

        Log($"OnTreeChanged — IsConnected={IsConnected}. Scheduling delayed TreeChangedEvent (350ms).");

        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for EnsureBuilt() to run (lazy — triggered on next layout frame).
                    // 350 ms is enough to cover one or two render frames at 60 fps.
                    await Task.Delay(350);
                    Log("OnTreeChanged — delay elapsed, sending TreeChangedEvent.");
                    await SendMessageAsync(new TreeChangedEvent());
                    Log("OnTreeChanged — TreeChangedEvent sent.");
                }
                catch (Exception ex)
                {
                    Log($"OnTreeChanged — error sending TreeChangedEvent: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Called when overlays are added or removed.
    /// No debouncing - overlay changes are infrequent and important to track.
    /// </summary>
    private void OnOverlaysChanged()
    {
        Log($"OnOverlaysChanged — IsConnected={IsConnected}.");
        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendMessageAsync(new OverlaysChangedEvent());
                    Log("OnOverlaysChanged — OverlaysChangedEvent sent.");
                }
                catch (Exception ex)
                {
                    Log($"OnOverlaysChanged — error: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Called when elements are added/removed from the tree.
    /// Determines if the change is in the main tree or an overlay and sends appropriate notification.
    /// </summary>
    private void OnTreeStructureChanged(VisualElement changedElement)
    {
        // Early exit if no client connected - avoid expensive parent chain traversal
        if (!IsConnected) return;

        // Check if the changed element is part of the main tree or an overlay
        bool isInMainTree = IsElementInMainTree(changedElement);
        bool isInOverlay = IsElementInOverlay(changedElement);

        if (!isInMainTree && !isInOverlay)
        {
            return; // Element is not visible in either tree, skip notification
        }

        // Only debounce main tree structure changes (overlays handled separately)
        if (isInMainTree)
        {
            var now = DateTime.UtcNow;
            if (now - _lastTreeStructureNotification < _treeChangeDebounceInterval)
            {
                return; // Debounce: skip if too soon after last notification
            }
            _lastTreeStructureNotification = now;
        }

        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (isInMainTree)
                    {
                        await SendMessageAsync(new TreeChangedEvent());
                        Log("OnTreeStructureChanged — TreeChangedEvent sent (main tree).");
                    }
                    else if (isInOverlay)
                    {
                        await SendMessageAsync(new OverlaysChangedEvent());
                        Log("OnTreeStructureChanged — OverlaysChangedEvent sent (overlay).");
                    }
                }
                catch (Exception ex)
                {
                    Log($"OnTreeStructureChanged — error: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Checks if an element is part of the main UI tree (has Root as ancestor).
    /// </summary>
    private bool IsElementInMainTree(VisualElement element)
    {
        var root = _uiTree.Root;
        if (root == null) return false;

        var current = element;
        while (current != null)
        {
            if (ReferenceEquals(current, root)) return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Checks if an element is still reachable (part of main tree or overlay).
    /// </summary>
    private bool IsElementReachable(VisualElement element)
    {
        return IsElementInMainTree(element) || IsElementInOverlay(element);
    }

    /// <summary>
    /// Checks if an element is part of an overlay.
    /// </summary>
    private bool IsElementInOverlay(VisualElement element)
    {
        foreach (var overlay in _uiTree.Overlays)
        {
            var current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, overlay)) return true;
                current = current.Parent;
            }
        }
        return false;
    }

    private void SendLogMessage(string level, string message)
    {
        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendMessageAsync(new LogMessage { Level = level, Message = message, Timestamp = DateTime.UtcNow });
                }
                catch (Exception ex)
                {
                    Log($"SendLogMessage — error: {ex.Message}");
                }
            });
        }
    }

    public void Dispose()
    {
        _uiTree.RootChanged -= OnTreeChanged;
        _uiTree.OverlaysChanged -= OnOverlaysChanged;
        VisualElement.TreeStructureChanged -= OnTreeStructureChanged;
        DevToolLogger.OnLog -= SendLogMessage;
        Stop();
    }
}
