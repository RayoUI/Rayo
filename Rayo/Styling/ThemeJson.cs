namespace Rayo.Styling;

using System.Text.Json;
using System.Text.Json.Serialization;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>Versioned JSON import and export for ThemeData.</summary>
public static class ThemeJson
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(
        ThemeData theme,
        bool indented = true,
        ThemeJsonRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var generatedComponents = ComponentThemes.FromScheme(
            theme.Colors,
            theme.Buttons,
            theme.Typography,
            theme.Spacing,
            theme.Shapes,
            theme.Density,
            theme.Preferences);
        var document = ThemeJsonDocument.FromTheme(theme, generatedComponents, registry);
        var options = new JsonSerializerOptions(Options) { WriteIndented = indented };
        return JsonSerializer.Serialize(document, options);
    }

    public static ThemeData Deserialize(
        string json,
        Func<string, ThemeData?>? baseThemeResolver = null,
        ThemeJsonRegistry? registry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ThemeJsonDocument document;
        try
        {
            document = JsonSerializer.Deserialize<ThemeJsonDocument>(json, Options)
                ?? throw new ThemeJsonException("$", "The theme document is empty.");
        }
        catch (JsonException exception)
        {
            throw new ThemeJsonException(
                exception.Path ?? "$",
                exception.Message,
                exception);
        }

        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new ThemeJsonException(
                "$.schemaVersion",
                $"Unsupported schema version {document.SchemaVersion}; expected {CurrentSchemaVersion}.");
        }

        ThemeData? baseTheme = null;
        if (!string.IsNullOrWhiteSpace(document.BasedOn))
        {
            baseTheme = baseThemeResolver?.Invoke(document.BasedOn) ??
                ResolveBuiltIn(document.BasedOn);
            if (baseTheme == null)
            {
                throw new ThemeJsonException(
                    "$.basedOn",
                    $"Base theme '{document.BasedOn}' was not found.");
            }
        }

        return document.ToTheme(baseTheme, registry);
    }

    public static bool TryDeserialize(
        string json,
        out ThemeData? theme,
        out string? error,
        Func<string, ThemeData?>? baseThemeResolver = null,
        ThemeJsonRegistry? registry = null)
    {
        try
        {
            theme = Deserialize(json, baseThemeResolver, registry);
            error = null;
            return true;
        }
        catch (Exception exception) when (
            exception is ThemeJsonException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            theme = null;
            error = exception.Message;
            return false;
        }
    }

    private static ThemeData? ResolveBuiltIn(string name) =>
        name.Equals("light", StringComparison.OrdinalIgnoreCase)
            ? RayoThemes.Light
            : name.Equals("dark", StringComparison.OrdinalIgnoreCase)
                ? RayoThemes.Dark
                : name.Equals("high-contrast", StringComparison.OrdinalIgnoreCase)
                    ? RayoThemes.HighContrast
                : null;

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new ColorJsonConverter());
        options.Converters.Add(new ThicknessJsonConverter());
        options.Converters.Add(new CornerRadiusJsonConverter());
        return options;
    }

    private sealed record ThemeJsonDocument
    {
        public int SchemaVersion { get; init; }
        public string? BasedOn { get; init; }
        public required string Name { get; init; }
        public ThemeBrightness? Brightness { get; init; }
        public ColorScheme? Colors { get; init; }
        public TypographyScheme? Typography { get; init; }
        public SpacingScale? Spacing { get; init; }
        public ShapeScheme? Shapes { get; init; }
        public ElevationScheme? Elevation { get; init; }
        public MotionScheme? Motion { get; init; }
        public ThemeDensity? Density { get; init; }
        public ThemePreferences? Preferences { get; init; }
        public ButtonTheme? Buttons { get; init; }
        public IReadOnlyList<TokenJsonValue>? Tokens { get; init; }
        public IReadOnlyDictionary<string, JsonElement>? Extensions { get; init; }
        public IReadOnlyList<ComponentThemeJsonValue>? ComponentThemes { get; init; }

        public ThemeData ToTheme(ThemeData? baseTheme, ThemeJsonRegistry? registry)
        {
            var colors = Colors ?? baseTheme?.Colors ??
                throw new ThemeJsonException("$.colors", "A color scheme is required.");
            var components = baseTheme?.Components;
            if (Buttons != null && components != null)
                components = components with { Buttons = Buttons };
            var tokens = baseTheme?.Tokens ?? ThemeTokenSet.Empty;
            if (Tokens != null)
            {
                foreach (var token in Tokens)
                    tokens = token.Apply(tokens);
            }
            var extensions = baseTheme?.Extensions.ToDictionary(pair => pair.Key, pair => pair.Value)
                ?? new Dictionary<Type, IThemeExtension>();
            if (Extensions != null)
            {
                foreach (var (name, value) in Extensions)
                {
                    if (registry == null || !registry.TryDeserialize(name, value, Options, out var extension))
                    {
                        throw new ThemeJsonException(
                            $"$.extensions.{name}",
                            $"Theme extension '{name}' is not registered.");
                    }
                    extensions[extension.GetType()] = extension;
                }
            }

            var theme = new ThemeData(
                Name,
                colors,
                Buttons ?? baseTheme?.Buttons,
                Brightness ?? baseTheme?.Brightness ?? ThemeBrightness.Light,
                Typography ?? baseTheme?.Typography,
                Spacing ?? baseTheme?.Spacing,
                Shapes ?? baseTheme?.Shapes,
                Elevation ?? baseTheme?.Elevation,
                Motion ?? baseTheme?.Motion,
                Density ?? baseTheme?.Density ?? ThemeDensity.Comfortable,
                components: components,
                tokens: tokens,
                preferences: Preferences ?? baseTheme?.Preferences,
                extensions: extensions);
            if (ComponentThemes != null)
            {
                var updatedComponents = theme.Components;
                for (var index = 0; index < ComponentThemes.Count; index++)
                    updatedComponents = ComponentThemes[index].Apply(updatedComponents, index);
                theme = theme with { Components = updatedComponents };
                theme.Validate();
            }
            return theme;
        }

        public static ThemeJsonDocument FromTheme(
            ThemeData theme,
            ComponentThemes generatedComponents,
            ThemeJsonRegistry? registry)
        {
            Dictionary<string, JsonElement>? extensions = null;
            if (theme.Extensions.Count > 0)
            {
                extensions = new Dictionary<string, JsonElement>();
                foreach (var extension in theme.Extensions.Values)
                {
                    if (registry == null ||
                        !registry.TrySerialize(extension, Options, out var name, out var value))
                    {
                        throw new NotSupportedException(
                            $"Theme extension '{extension.GetType().FullName}' is not registered.");
                    }
                    extensions[name] = value;
                }
            }

            return new ThemeJsonDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                Name = theme.Name,
                Brightness = theme.Brightness,
                Colors = theme.Colors,
                Typography = theme.Typography,
                Spacing = theme.Spacing,
                Shapes = theme.Shapes,
                Elevation = theme.Elevation,
                Motion = theme.Motion,
                Density = theme.Density,
                Preferences = theme.Preferences,
                Buttons = theme.Buttons,
                Tokens = theme.Tokens.Snapshot().Select(TokenJsonValue.FromToken).ToArray(),
                Extensions = extensions,
                ComponentThemes = theme.Components
                    .GetDifferences(generatedComponents)
                    .Select(ComponentThemeJsonValue.FromTheme)
                    .ToArray(),
            };
        }
    }

    private sealed record ComponentThemeJsonValue
    {
        public required string Control { get; init; }
        public required IReadOnlyDictionary<string, ComponentPropertyJsonValue> Values { get; init; }

        public ComponentThemes Apply(ComponentThemes themes, int componentIndex)
        {
            var controlType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(Control, throwOnError: false))
                .FirstOrDefault(type => type != null);
            if (controlType == null)
            {
                throw new ThemeJsonException(
                    $"$.componentThemes[{componentIndex}].control",
                    $"Control type '{Control}' was not found.");
            }

            var values = new Dictionary<string, object?>();
            foreach (var (name, encoded) in Values)
            {
                var path = $"$.componentThemes[{componentIndex}].values.{name}";
                var property = controlType.GetProperty(name);
                if (property == null || !property.CanWrite)
                    throw new ThemeJsonException(path, $"Writable property '{name}' was not found.");
                values[name] = encoded.ToValue(property.PropertyType, path);
            }

            try
            {
                return themes.With(controlType, values);
            }
            catch (ArgumentException exception)
            {
                throw new ThemeJsonException(
                    $"$.componentThemes[{componentIndex}]",
                    exception.Message,
                    exception);
            }
        }

        public static ComponentThemeJsonValue FromTheme(
            (Type ControlType, IReadOnlyDictionary<string, object?> Values) component)
        {
            var controlName = component.ControlType.FullName
                ?? throw new NotSupportedException("Component control type has no stable full name.");
            return new ComponentThemeJsonValue
            {
                Control = controlName,
                Values = component.Values.ToDictionary(
                    pair => pair.Key,
                    pair => ComponentPropertyJsonValue.FromValue(
                        pair.Value,
                        $"{controlName}.{pair.Key}")),
            };
        }
    }

    private sealed record ComponentPropertyJsonValue
    {
        public string? Kind { get; init; }
        public required JsonElement Value { get; init; }

        public object? ToValue(Type propertyType, string path)
        {
            try
            {
                if (Value.ValueKind == JsonValueKind.Null)
                    return null;
                if (Kind == "solidBrush")
                {
                    if (!typeof(Brush).IsAssignableFrom(propertyType))
                        throw new ThemeJsonException(path, "A solid brush cannot be assigned here.");
                    var brush = Value.Deserialize<SolidBrushJsonValue>(Options)
                        ?? throw new ThemeJsonException(path, "The solid brush value is empty.");
                    return new SolidColorBrush(brush.Color) { Opacity = brush.Opacity };
                }
                return Value.Deserialize(propertyType, Options);
            }
            catch (JsonException exception)
            {
                throw new ThemeJsonException(path, exception.Message, exception);
            }
            catch (NotSupportedException exception)
            {
                throw new ThemeJsonException(path, exception.Message, exception);
            }
        }

        public static ComponentPropertyJsonValue FromValue(object? value, string path)
        {
            if (value is SolidColorBrush brush)
            {
                return new ComponentPropertyJsonValue
                {
                    Kind = "solidBrush",
                    Value = JsonSerializer.SerializeToElement(
                        new SolidBrushJsonValue(brush.Color, brush.Opacity),
                        Options),
                };
            }
            if (value is Brush)
            {
                throw new NotSupportedException(
                    $"Component property '{path}' uses an unsupported brush type.");
            }

            return new ComponentPropertyJsonValue
            {
                Value = value == null
                    ? JsonSerializer.SerializeToElement<object?>(null, Options)
                    : JsonSerializer.SerializeToElement(value, value.GetType(), Options),
            };
        }
    }

    private sealed record SolidBrushJsonValue(Color Color, float Opacity);

    private sealed record TokenJsonValue
    {
        public required string Name { get; init; }
        public required string Type { get; init; }
        public required JsonElement Value { get; init; }

        public ThemeTokenSet Apply(ThemeTokenSet tokens) => Type switch
        {
            "string" => tokens.Set(new ThemeKey<string>(Name), Value.GetString() ?? string.Empty),
            "float" => tokens.Set(new ThemeKey<float>(Name), Value.GetSingle()),
            "double" => tokens.Set(new ThemeKey<double>(Name), Value.GetDouble()),
            "int" => tokens.Set(new ThemeKey<int>(Name), Value.GetInt32()),
            "bool" => tokens.Set(new ThemeKey<bool>(Name), Value.GetBoolean()),
            "color" => tokens.Set(
                new ThemeKey<Color>(Name),
                Value.Deserialize<Color>(Options)),
            _ => throw new ThemeJsonException(
                $"$.tokens[{Name}]",
                $"Unsupported token type '{Type}'."),
        };

        public static TokenJsonValue FromToken(ThemeTokenValue token)
        {
            var type = token.ValueType == typeof(string) ? "string"
                : token.ValueType == typeof(float) ? "float"
                : token.ValueType == typeof(double) ? "double"
                : token.ValueType == typeof(int) ? "int"
                : token.ValueType == typeof(bool) ? "bool"
                : token.ValueType == typeof(Color) ? "color"
                : throw new NotSupportedException(
                    $"Token '{token.Name}' has unsupported JSON type {token.ValueType.Name}.");
            return new TokenJsonValue
            {
                Name = token.Name,
                Type = type,
                Value = JsonSerializer.SerializeToElement(token.Value, token.ValueType, Options),
            };
        }
    }

    private sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var text = reader.GetString();
            if (text == null || (text.Length != 7 && text.Length != 9) || text[0] != '#')
                throw new JsonException("Colors must use #RRGGBB or #RRGGBBAA.");
            try
            {
                var r = Convert.ToByte(text.Substring(1, 2), 16);
                var g = Convert.ToByte(text.Substring(3, 2), 16);
                var b = Convert.ToByte(text.Substring(5, 2), 16);
                var a = text.Length == 9 ? Convert.ToByte(text.Substring(7, 2), 16) : (byte)255;
                return new Color(r, g, b, a);
            }
            catch (FormatException exception)
            {
                throw new JsonException("Color contains invalid hexadecimal digits.", exception);
            }
        }

        public override void Write(
            Utf8JsonWriter writer,
            Color value,
            JsonSerializerOptions options)
        {
            static byte Byte(float channel) => (byte)Math.Clamp(
                (int)MathF.Round(channel * 255f),
                0,
                255);
            writer.WriteStringValue(
                $"#{Byte(value.R):X2}{Byte(value.G):X2}{Byte(value.B):X2}{Byte(value.A):X2}");
        }
    }

    private sealed class ThicknessJsonConverter : JsonConverter<Thickness>
    {
        public override Thickness Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            return new Thickness(
                root.GetProperty("left").GetSingle(),
                root.GetProperty("top").GetSingle(),
                root.GetProperty("right").GetSingle(),
                root.GetProperty("bottom").GetSingle());
        }

        public override void Write(
            Utf8JsonWriter writer,
            Thickness value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("left", value.Left);
            writer.WriteNumber("top", value.Top);
            writer.WriteNumber("right", value.Right);
            writer.WriteNumber("bottom", value.Bottom);
            writer.WriteEndObject();
        }
    }

    private sealed class CornerRadiusJsonConverter : JsonConverter<CornerRadius>
    {
        public override CornerRadius Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            return new CornerRadius(
                root.GetProperty("topLeft").GetSingle(),
                root.GetProperty("topRight").GetSingle(),
                root.GetProperty("bottomRight").GetSingle(),
                root.GetProperty("bottomLeft").GetSingle());
        }

        public override void Write(
            Utf8JsonWriter writer,
            CornerRadius value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("topLeft", value.TopLeft);
            writer.WriteNumber("topRight", value.TopRight);
            writer.WriteNumber("bottomRight", value.BottomRight);
            writer.WriteNumber("bottomLeft", value.BottomLeft);
            writer.WriteEndObject();
        }
    }
}

/// <summary>
/// Maps stable JSON identifiers to application-specific theme extension types.
/// Registries are explicit and instance-based so serialization has no mutable global state.
/// </summary>
public sealed class ThemeJsonRegistry
{
    private readonly Dictionary<string, Type> _typesByName =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, string> _namesByType = new();

    public ThemeJsonRegistry Register<T>(string name)
        where T : class, IThemeExtension
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_typesByName.TryGetValue(name, out var existingType) && existingType != typeof(T))
            throw new ArgumentException($"Extension name '{name}' is already registered.", nameof(name));
        if (_namesByType.TryGetValue(typeof(T), out var existingName) &&
            !existingName.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Extension type '{typeof(T).FullName}' is already registered as '{existingName}'.",
                nameof(name));
        }

        _typesByName[name] = typeof(T);
        _namesByType[typeof(T)] = name;
        return this;
    }

    internal bool TrySerialize(
        IThemeExtension extension,
        JsonSerializerOptions options,
        out string name,
        out JsonElement value)
    {
        if (!_namesByType.TryGetValue(extension.GetType(), out name!))
        {
            value = default;
            return false;
        }

        value = JsonSerializer.SerializeToElement(extension, extension.GetType(), options);
        return true;
    }

    internal bool TryDeserialize(
        string name,
        JsonElement value,
        JsonSerializerOptions options,
        out IThemeExtension extension)
    {
        if (!_typesByName.TryGetValue(name, out var type))
        {
            extension = null!;
            return false;
        }

        try
        {
            extension = (IThemeExtension)(value.Deserialize(type, options)
                ?? throw new ThemeJsonException(
                    $"$.extensions.{name}",
                    "The extension value cannot be null."));
            return true;
        }
        catch (JsonException exception)
        {
            throw new ThemeJsonException(
                $"$.extensions.{name}{exception.Path}",
                exception.Message,
                exception);
        }
    }
}

public sealed class ThemeJsonException : Exception
{
    public string JsonPath { get; }

    public ThemeJsonException(string jsonPath, string message, Exception? inner = null)
        : base($"{jsonPath}: {message}", inner)
    {
        JsonPath = jsonPath;
    }
}

/// <summary>
/// Watches a JSON theme and atomically publishes only successfully validated updates.
/// </summary>
public sealed class ThemeHotReloadWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Timer _debounce;
    private readonly Func<string, ThemeData?>? _baseThemeResolver;
    private readonly ThemeJsonRegistry? _registry;
    private readonly string _path;

    public ThemeData Current { get; private set; }
    public event Action<ThemeData>? ThemeChanged;
    public event Action<string>? LoadFailed;

    public ThemeHotReloadWatcher(
        string path,
        Func<string, ThemeData?>? baseThemeResolver = null,
        ThemeJsonRegistry? registry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Theme path has no directory.", nameof(path));
        var fileName = Path.GetFileName(fullPath);
        _path = fullPath;
        _baseThemeResolver = baseThemeResolver;
        _registry = registry;
        Current = ThemeJson.Deserialize(File.ReadAllText(fullPath), baseThemeResolver, registry);
        _debounce = new Timer(_ => Reload(fullPath), null, Timeout.Infinite, Timeout.Infinite);
        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnChanged;
    }

    private void OnChanged(object sender, FileSystemEventArgs args) =>
        _debounce.Change(100, Timeout.Infinite);

    private void Reload(string path)
    {
        _ = path;
        TryReload(out _);
    }

    /// <summary>
    /// Reloads synchronously. A failed load leaves <see cref="Current"/> unchanged.
    /// </summary>
    public bool TryReload(out string? error)
    {
        try
        {
            var next = ThemeJson.Deserialize(File.ReadAllText(_path), _baseThemeResolver, _registry);
            Current = next;
            ThemeChanged?.Invoke(next);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            LoadFailed?.Invoke(error);
            return false;
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounce.Dispose();
    }
}
