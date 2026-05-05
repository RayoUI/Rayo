using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rayo.DevTool.Shared.Protocol;

/// <summary>
/// Base class for all DevTool messages
/// </summary>
public abstract class DevToolMessage
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
}

// ============================================================================
// REQUEST MESSAGES (DevTool -> Agent)
// ============================================================================

/// <summary>
/// Request the full UI tree
/// </summary>
public class GetTreeRequest : DevToolMessage
{
    public override string Type => "get_tree";
}

/// <summary>
/// Request only the overlays
/// </summary>
public class GetOverlaysRequest : DevToolMessage
{
    public override string Type => "get_overlays";
}

/// <summary>
/// Request properties for a specific element
/// </summary>
public class GetPropertiesRequest : DevToolMessage
{
    public override string Type => "get_properties";

    [JsonPropertyName("elementId")]
    public string ElementId { get; set; } = "";
}

/// <summary>
/// Set a property value on an element
/// </summary>
public class SetPropertyRequest : DevToolMessage
{
    public override string Type => "set_property";

    [JsonPropertyName("elementId")]
    public string ElementId { get; set; } = "";

    [JsonPropertyName("propertyName")]
    public string PropertyName { get; set; } = "";

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}

/// <summary>
/// Highlight an element in the running application
/// </summary>
public class HighlightElementRequest : DevToolMessage
{
    public override string Type => "highlight";

    [JsonPropertyName("elementId")]
    public string? ElementId { get; set; }
}

// ============================================================================
// RESPONSE MESSAGES (Agent -> DevTool)
// ============================================================================

/// <summary>
/// Response containing the UI tree
/// </summary>
public class TreeResponse : DevToolMessage
{
    public override string Type => "tree";

    [JsonPropertyName("root")]
    public ElementNode? Root { get; set; }

    [JsonPropertyName("overlays")]
    public List<ElementNode> Overlays { get; set; } = new();
}

/// <summary>
/// Response containing only the overlays
/// </summary>
public class OverlaysResponse : DevToolMessage
{
    public override string Type => "overlays";

    [JsonPropertyName("overlays")]
    public List<ElementNode> Overlays { get; set; } = new();
}

/// <summary>
/// Response containing element properties
/// </summary>
public class PropertiesResponse : DevToolMessage
{
    public override string Type => "properties";

    [JsonPropertyName("elementId")]
    public string ElementId { get; set; } = "";

    [JsonPropertyName("properties")]
    public List<PropertyInfo> Properties { get; set; } = new();
}

/// <summary>
/// Generic success/error response
/// </summary>
public class ResultResponse : DevToolMessage
{
    public override string Type => "result";

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = "";
}

/// <summary>
/// Event notification when main tree changes (hot reload)
/// </summary>
public class TreeChangedEvent : DevToolMessage
{
    public override string Type => "tree_changed";
}

/// <summary>
/// Event notification when overlays change (Drawer, Dialog, etc.)
/// </summary>
public class OverlaysChangedEvent : DevToolMessage
{
    public override string Type => "overlays_changed";
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

/// <summary>
/// Represents a node in the UI tree
/// </summary>
public class ElementNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string TypeName { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("width")]
    public float Width { get; set; }

    [JsonPropertyName("height")]
    public float Height { get; set; }

    [JsonPropertyName("visible")]
    public bool IsVisible { get; set; } = true;

    [JsonPropertyName("childCount")]
    public int ChildCount { get; set; }

    [JsonPropertyName("children")]
    public List<ElementNode> Children { get; set; } = new();
}

/// <summary>
/// Information about a single property
/// </summary>
public class PropertyInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string TypeName { get; set; } = "";

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("isReadOnly")]
    public bool IsReadOnly { get; set; }

    [JsonPropertyName("editor")]
    public string? Editor { get; set; } // "text", "number", "color", "boolean", "enum"

    [JsonPropertyName("enumValues")]
    public List<string>? EnumValues { get; set; }
}

// ============================================================================
// MESSAGE SERIALIZATION
// ============================================================================

public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static string Serialize(DevToolMessage message)
    {
        return JsonSerializer.Serialize(message, message.GetType(), Options);
    }

    public static DevToolMessage? Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("type").GetString();

        return type switch
        {
            "get_tree" => JsonSerializer.Deserialize<GetTreeRequest>(json, Options),
            "get_overlays" => JsonSerializer.Deserialize<GetOverlaysRequest>(json, Options),
            "get_properties" => JsonSerializer.Deserialize<GetPropertiesRequest>(json, Options),
            "set_property" => JsonSerializer.Deserialize<SetPropertyRequest>(json, Options),
            "highlight" => JsonSerializer.Deserialize<HighlightElementRequest>(json, Options),
            "get_perf_stats"    => JsonSerializer.Deserialize<GetPerformanceStatsRequest>(json, Options),
            "set_dirty_heatmap" => JsonSerializer.Deserialize<SetDirtyHeatmapRequest>(json, Options),
            "set_overdraw"      => JsonSerializer.Deserialize<SetOverdrawVisualizerRequest>(json, Options),
            "set_extended_stats"=> JsonSerializer.Deserialize<SetExtendedStatsRequest>(json, Options),
            "clear_dirty_log"   => JsonSerializer.Deserialize<ClearDirtyLogRequest>(json, Options),
            "perf_stats"        => JsonSerializer.Deserialize<PerformanceStatsResponse>(json, Options),
            "tree" => JsonSerializer.Deserialize<TreeResponse>(json, Options),
            "overlays" => JsonSerializer.Deserialize<OverlaysResponse>(json, Options),
            "properties" => JsonSerializer.Deserialize<PropertiesResponse>(json, Options),
            "result" => JsonSerializer.Deserialize<ResultResponse>(json, Options),
            "tree_changed" => JsonSerializer.Deserialize<TreeChangedEvent>(json, Options),
            "overlays_changed" => JsonSerializer.Deserialize<OverlaysChangedEvent>(json, Options),
            "log_message" => JsonSerializer.Deserialize<LogMessage>(json, Options),
            _ => null
        };
    }
}

/// <summary>
/// Log message from the application
/// </summary>
public class LogMessage : DevToolMessage
{
    public override string Type => "log_message";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("level")]
    public string Level { get; set; } = "Info";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
