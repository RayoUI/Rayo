namespace Rayo.Styling;

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Rayo.Controls;
using Rayo.Core;

public enum ThemeBrightness
{
    Light,
    Dark,
    HighContrast,
}

public enum ThemeMode
{
    Light,
    Dark,
    System,
}

public enum ThemeDensity
{
    Compact,
    Comfortable,
    Touch,
}

/// <summary>Accessibility and appearance preferences reported by a platform host.</summary>
public sealed record HostThemePreferences
{
    public bool PrefersDark { get; init; }
    public bool HighContrast { get; init; }
    public bool ReduceMotion { get; init; }
    public float TextScale { get; init; } = 1f;
    public ThemeDensity Density { get; init; } = ThemeDensity.Comfortable;

    public static HostThemePreferences Default { get; } = new();

    public void Validate()
    {
        if (TextScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(TextScale), "Text scale must be positive.");
    }
}

public sealed record ThemePreferences
{
    public bool HighContrast { get; init; }
    public bool ReduceMotion { get; init; }
    public float TextScale { get; init; } = 1f;

    public static ThemePreferences Default { get; } = new();

    internal void Validate()
    {
        if (TextScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(TextScale), "Text scale must be positive.");
    }
}

public sealed record TypographyStyle
{
    public string? FontFamily { get; init; }
    public float FontSize { get; init; }
    public FontWeight FontWeight { get; init; } = FontWeight.Normal;
    public float LineHeight { get; init; } = 1.2f;
    public float LetterSpacing { get; init; }

    internal void Validate(string name)
    {
        if (FontSize <= 0)
            throw new ArgumentOutOfRangeException(name, "Font size must be positive.");
        if (LineHeight <= 0)
            throw new ArgumentOutOfRangeException(name, "Line height must be positive.");
    }
}

public sealed record TypographyScheme
{
    public TypographyStyle Display { get; init; } =
        new() { FontSize = 32, FontWeight = FontWeight.Bold, LineHeight = 1.15f };
    public TypographyStyle Heading { get; init; } =
        new() { FontSize = 24, FontWeight = FontWeight.SemiBold, LineHeight = 1.2f };
    public TypographyStyle Title { get; init; } =
        new() { FontSize = 18, FontWeight = FontWeight.SemiBold, LineHeight = 1.25f };
    public TypographyStyle Body { get; init; } =
        new() { FontSize = 14, LineHeight = 1.4f };
    public TypographyStyle Label { get; init; } =
        new() { FontSize = 14, FontWeight = FontWeight.Medium, LineHeight = 1.2f };
    public TypographyStyle Caption { get; init; } =
        new() { FontSize = 12, LineHeight = 1.3f };
    public TypographyStyle Code { get; init; } =
        new() { FontFamily = "monospace", FontSize = 13, LineHeight = 1.35f };

    public static TypographyScheme Default { get; } = new();

    internal void Validate()
    {
        Display.Validate(nameof(Display));
        Heading.Validate(nameof(Heading));
        Title.Validate(nameof(Title));
        Body.Validate(nameof(Body));
        Label.Validate(nameof(Label));
        Caption.Validate(nameof(Caption));
        Code.Validate(nameof(Code));
    }
}

public sealed record SpacingScale
{
    public float None { get; init; }
    public float Xs { get; init; } = 2;
    public float Sm { get; init; } = 4;
    public float Md { get; init; } = 8;
    public float Lg { get; init; } = 12;
    public float Xl { get; init; } = 16;
    public float Xxl { get; init; } = 24;
    public float Xxxl { get; init; } = 32;

    public static SpacingScale Default { get; } = new();

    internal void Validate()
    {
        var values = new[] { None, Xs, Sm, Md, Lg, Xl, Xxl, Xxxl };
        if (values.Any(value => value < 0))
            throw new ArgumentOutOfRangeException(nameof(SpacingScale), "Spacing cannot be negative.");
        for (var index = 1; index < values.Length; index++)
        {
            if (values[index] < values[index - 1])
                throw new ArgumentException("Spacing values must be monotonic.", nameof(SpacingScale));
        }
    }
}

public sealed record ShapeScheme
{
    public CornerRadius None { get; init; } = CornerRadius.None;
    public CornerRadius Small { get; init; } = new(2);
    public CornerRadius Medium { get; init; } = new(4);
    public CornerRadius Large { get; init; } = new(8);
    public CornerRadius Pill { get; init; } = new(999);
    public float BorderThin { get; init; } = 1;
    public float BorderThick { get; init; } = 2;

    public static ShapeScheme Default { get; } = new();

    internal void Validate()
    {
        if (BorderThin < 0 || BorderThick < 0)
            throw new ArgumentOutOfRangeException(nameof(ShapeScheme), "Border widths cannot be negative.");
    }
}

public sealed record ElevationLevel
{
    public float Blur { get; init; }
    public float OffsetY { get; init; }
    public float Opacity { get; init; }
}

public sealed record ElevationScheme
{
    public ElevationLevel None { get; init; } = new();
    public ElevationLevel Low { get; init; } = new() { Blur = 4, OffsetY = 1, Opacity = 0.12f };
    public ElevationLevel Medium { get; init; } = new() { Blur = 12, OffsetY = 4, Opacity = 0.18f };
    public ElevationLevel High { get; init; } = new() { Blur = 24, OffsetY = 8, Opacity = 0.24f };

    public static ElevationScheme Default { get; } = new();

    internal void Validate()
    {
        foreach (var level in new[] { None, Low, Medium, High })
        {
            if (level.Blur < 0 || level.Opacity is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(ElevationScheme));
        }
    }
}

public sealed record MotionScheme
{
    public TimeSpan Fast { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan Normal { get; init; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan Slow { get; init; } = TimeSpan.FromMilliseconds(300);

    public static MotionScheme Default { get; } = new();

    internal void Validate()
    {
        if (Fast < TimeSpan.Zero || Normal < TimeSpan.Zero || Slow < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MotionScheme));
    }
}

public interface IThemeExtension
{
}

public sealed record ComponentThemes
{
    public ButtonTheme Buttons { get; init; }
    private IReadOnlyDictionary<Type, IComponentTheme> ControlThemes { get; init; }

    public ComponentThemes(
        ButtonTheme buttons,
        IReadOnlyDictionary<Type, IComponentTheme>? controlThemes = null)
    {
        Buttons = buttons ?? throw new ArgumentNullException(nameof(buttons));
        ControlThemes = controlThemes ?? EmptyControlThemes;
    }

    public static ComponentThemes FromScheme(
        ColorScheme colors,
        ButtonTheme? buttons = null,
        TypographyScheme? typography = null,
        SpacingScale? spacing = null,
        ShapeScheme? shapes = null,
        ThemeDensity density = ThemeDensity.Comfortable,
        ThemePreferences? preferences = null)
    {
        typography ??= TypographyScheme.Default;
        spacing ??= SpacingScale.Default;
        shapes ??= ShapeScheme.Default;
        preferences ??= ThemePreferences.Default;
        var controlHeight = density switch
        {
            ThemeDensity.Compact => 28f,
            ThemeDensity.Touch => 44f,
            _ => 36f,
        };
        var inputPadding = density switch
        {
            ThemeDensity.Compact => new Thickness(8, 4),
            ThemeDensity.Touch => new Thickness(14, 10),
            _ => new Thickness(10, 6),
        };

        return new ComponentThemes(buttons ?? ButtonTheme.FromScheme(colors))
            .With(ComponentTheme<Entry>.Empty
                .Set(control => control.FontSize, typography.Body.FontSize * preferences.TextScale)
                .Set(control => control.Padding, inputPadding)
                .Set(control => control.BorderThickness, new Thickness(shapes.BorderThick))
                .Set(control => control.BorderRadius, shapes.Medium)
                .Set(control => control.MinHeight, controlHeight))
            .With(ComponentTheme<Editor>.Empty
                .Set(control => control.FontSize, typography.Body.FontSize * preferences.TextScale)
                .Set(control => control.Padding, inputPadding)
                .Set(control => control.BorderThickness, new Thickness(shapes.BorderThick))
                .Set(control => control.BorderRadius, shapes.Medium)
                .Set(control => control.MinHeight, controlHeight))
            .With(ComponentTheme<ComboBox>.Empty
                .Set(control => control.Width, 200f)
                .Set(control => control.Height, controlHeight)
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<DatePicker>.Empty
                .Set(control => control.Width, 240f)
                .Set(control => control.Height, controlHeight)
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<TimePicker>.Empty
                .Set(control => control.Width, 180f)
                .Set(control => control.Height, controlHeight)
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<PathPicker>.Empty
                .Set(control => control.Width, 320f)
                .Set(control => control.Height, controlHeight)
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<Stepper>.Empty
                .Set(control => control.Width, 140f)
                .Set(control => control.Height, controlHeight)
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<Slider>.Empty
                .Set(control => control.Width, 200f)
                .Set(control => control.Height, density == ThemeDensity.Compact ? 24f : density == ThemeDensity.Touch ? 36f : 30f))
            .With(ComponentTheme<ProgressBar>.Empty
                .Set(control => control.Height, density == ThemeDensity.Compact ? 3f : density == ThemeDensity.Touch ? 6f : 4f))
            .With(ComponentTheme<DataGrid>.Empty
                .Set(control => control.Width, 600f)
                .Set(control => control.Height, 400f)
                .Set(control => control.BorderRadius, shapes.Large))
            .With(ComponentTheme<Card>.Empty
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<Expander>.Empty
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<ButtonGroup>.Empty
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<Checkbox>.Empty
                .Set(control => control.BoxSize, density == ThemeDensity.Touch ? 22f : 18f)
                .Set(control => control.LabelSpacing, spacing.Md)
                .Set(control => control.BoxPadding, shapes.BorderThick)
                .Set(control => control.FontSize, typography.Body.FontSize * preferences.TextScale))
            .With(ComponentTheme<RadioButton>.Empty
                .Set(control => control.CircleSize, density == ThemeDensity.Touch ? 22f : 18f)
                .Set(control => control.LabelSpacing, spacing.Md)
                .Set(control => control.CirclePadding, spacing.Xs)
                .Set(control => control.FontSize, typography.Body.FontSize * preferences.TextScale)
                .Set(control => control.BorderThickness, new Thickness(shapes.BorderThick)))
            .With(ComponentTheme<ToggleSwitch>.Empty
                .Set(control => control.SwitchWidth, density == ThemeDensity.Compact ? 40f : density == ThemeDensity.Touch ? 56f : 50f)
                .Set(control => control.SwitchHeight, density == ThemeDensity.Compact ? 22f : density == ThemeDensity.Touch ? 32f : 26f)
                .Set(control => control.ThumbSize, density == ThemeDensity.Compact ? 16f : density == ThemeDensity.Touch ? 26f : 20f)
                .Set(control => control.BorderThickness, shapes.BorderThick))
            .With(ComponentTheme<TabControl>.Empty
                .Set(control => control.TabHeight, controlHeight)
                .Set(control => control.TabWidth, density == ThemeDensity.Compact ? 100f : density == ThemeDensity.Touch ? 136f : 120f)
                .Set(control => control.ScrollButtonWidth, density == ThemeDensity.Touch ? 36f : 28f)
                .Set(control => control.TabCloseButtonSize, typography.Caption.FontSize * preferences.TextScale)
                .Set(control => control.TabCloseButtonHitSize, density == ThemeDensity.Touch ? 28f : 20f))
            .With(ComponentTheme<SideBar>.Empty
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<TreeView>.Empty
                .Set(control => control.BorderThickness, shapes.BorderThin))
            .With(ComponentTheme<Icon>.Empty
                .Set(control => control.Width, spacing.Xxl)
                .Set(control => control.Height, spacing.Xxl))
            .With(ComponentTheme<Loading>.Empty
                .Set(control => control.Width, density == ThemeDensity.Compact ? 32f : density == ThemeDensity.Touch ? 48f : 40f)
                .Set(control => control.Height, density == ThemeDensity.Compact ? 32f : density == ThemeDensity.Touch ? 48f : 40f))
            .With(ComponentTheme<Link>.Empty
                .Set(control => control.Padding, new Thickness(spacing.None)))
            .With(ComponentTheme<TooltipFrame>.Empty
                .Set(control => control.Padding, new Thickness(spacing.Lg, spacing.Xl, spacing.Lg, spacing.Md))
                .Set(control => control.BorderRadius, shapes.Medium));
    }

    public ComponentTheme<Entry> Entries => For<Entry>();
    public ComponentTheme<Editor> Editors => For<Editor>();
    public ComponentTheme<Checkbox> Checkboxes => For<Checkbox>();
    public ComponentTheme<RadioButton> RadioButtons => For<RadioButton>();
    public ComponentTheme<ToggleSwitch> ToggleSwitches => For<ToggleSwitch>();
    public ComponentTheme<ComboBox> ComboBoxes => For<ComboBox>();
    public ComponentTheme<Slider> Sliders => For<Slider>();
    public ComponentTheme<ProgressBar> ProgressBars => For<ProgressBar>();
    public ComponentTheme<DataGrid> DataGrids => For<DataGrid>();
    public ComponentTheme<TabControl> Tabs => For<TabControl>();
    public ComponentTheme<TooltipHost> Tooltips => For<TooltipHost>();

    internal void Validate()
    {
        Buttons.Validate();
    }

    public ComponentThemes With<TControl>(ComponentTheme<TControl> theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var themes = new Dictionary<Type, IComponentTheme>(ControlThemes)
        {
            [typeof(TControl)] = theme,
        };
        return this with
        {
            ControlThemes = new ReadOnlyDictionary<Type, IComponentTheme>(themes),
        };
    }

    public ComponentTheme<TControl> For<TControl>() =>
        ControlThemes.TryGetValue(typeof(TControl), out var theme)
            ? (ComponentTheme<TControl>)theme
            : ComponentTheme<TControl>.Empty;

    public bool HasCustomThemes => ControlThemes.Count > 0;

    internal bool ContentEquals(ComponentThemes other)
    {
        if (ControlThemes.Count != other.ControlThemes.Count)
            return false;

        foreach (var (controlType, theme) in ControlThemes)
        {
            if (!other.ControlThemes.TryGetValue(controlType, out var otherTheme))
                return false;

            var values = theme.Values.ToDictionary(pair => pair.Key, pair => pair.Value);
            var otherValues = otherTheme.Values.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (values.Count != otherValues.Count)
                return false;
            foreach (var (name, value) in values)
            {
                if (!otherValues.TryGetValue(name, out var otherValue) ||
                    !Equals(value, otherValue))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal IEnumerable<(Type ControlType, IReadOnlyDictionary<string, object?> Values)>
        GetDifferences(ComponentThemes baseline)
    {
        foreach (var (controlType, theme) in ControlThemes)
        {
            var baselineValues = baseline.ControlThemes.TryGetValue(controlType, out var baselineTheme)
                ? baselineTheme.Values.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>();
            var differences = theme.Values
                .Where(pair =>
                    !baselineValues.TryGetValue(pair.Key, out var baselineValue) ||
                    !Equals(pair.Value, baselineValue))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            if (differences.Count > 0)
                yield return (controlType, differences);
        }
    }

    internal ComponentThemes With(
        Type controlType,
        IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(controlType);
        ArgumentNullException.ThrowIfNull(values);

        var entries = ControlThemes.TryGetValue(controlType, out var existing)
            ? existing.Entries.ToDictionary(entry => entry.Name)
            : new Dictionary<string, ComponentThemeEntry>();
        foreach (var (name, value) in values)
        {
            var property = controlType.GetProperty(name)
                ?? throw new ArgumentException(
                    $"Control '{controlType.FullName}' has no public property '{name}'.",
                    nameof(values));
            if (!property.CanWrite)
                throw new ArgumentException(
                    $"Control property '{controlType.FullName}.{name}' is read-only.",
                    nameof(values));
            if (value != null && !property.PropertyType.IsInstanceOfType(value))
                throw new ArgumentException(
                    $"Value for '{controlType.FullName}.{name}' must be {property.PropertyType.Name}.",
                    nameof(values));
            entries[name] = new ComponentThemeEntry(name, property.PropertyType, value);
        }

        var themes = new Dictionary<Type, IComponentTheme>(ControlThemes)
        {
            [controlType] = new RuntimeComponentTheme(entries.Values),
        };
        return this with
        {
            ControlThemes = new ReadOnlyDictionary<Type, IComponentTheme>(themes),
        };
    }

    internal T Resolve<T>(Type controlType, string propertyName, T fallback)
    {
        for (var type = controlType; type != null; type = type.BaseType)
        {
            if (ControlThemes.TryGetValue(type, out var theme) &&
                theme.TryGet(propertyName, typeof(T), out var value))
            {
                return (T)value!;
            }
        }
        return fallback;
    }

    internal IEnumerable<KeyValuePair<string, object?>> GetOverrides(Type controlType)
    {
        var hierarchy = new Stack<Type>();
        for (var type = controlType; type != null; type = type.BaseType)
            hierarchy.Push(type);

        var values = new Dictionary<string, object?>();
        while (hierarchy.Count > 0)
        {
            if (!ControlThemes.TryGetValue(hierarchy.Pop(), out var theme))
                continue;
            foreach (var pair in theme.Values)
                values[pair.Key] = pair.Value;
        }
        return values;
    }

    private static IReadOnlyDictionary<Type, IComponentTheme> EmptyControlThemes { get; } =
        new ReadOnlyDictionary<Type, IComponentTheme>(
            new Dictionary<Type, IComponentTheme>());
}

public interface IComponentTheme
{
    bool TryGet(string propertyName, Type valueType, out object? value);
    IEnumerable<KeyValuePair<string, object?>> Values { get; }
    IEnumerable<ComponentThemeEntry> Entries { get; }
}

public sealed record ComponentThemeEntry(string Name, Type ValueType, object? Value);

internal sealed class RuntimeComponentTheme : IComponentTheme
{
    private readonly IReadOnlyDictionary<string, ComponentThemeEntry> _entries;

    public RuntimeComponentTheme(IEnumerable<ComponentThemeEntry> entries)
    {
        _entries = entries.ToDictionary(entry => entry.Name);
    }

    public bool TryGet(string propertyName, Type valueType, out object? value)
    {
        if (_entries.TryGetValue(propertyName, out var entry) &&
            valueType.IsAssignableFrom(entry.ValueType))
        {
            value = entry.Value;
            return true;
        }
        value = null;
        return false;
    }

    public IEnumerable<KeyValuePair<string, object?>> Values =>
        _entries.Values.Select(entry =>
            new KeyValuePair<string, object?>(entry.Name, entry.Value));

    public IEnumerable<ComponentThemeEntry> Entries => _entries.Values;
}

/// <summary>
/// Immutable, compile-time checked property overrides for any built-in or third-party control.
/// </summary>
public sealed class ComponentTheme<TControl> : IComponentTheme
{
    private readonly IReadOnlyDictionary<string, Entry> _values;

    public static ComponentTheme<TControl> Empty { get; } = new(
        new ReadOnlyDictionary<string, Entry>(new Dictionary<string, Entry>()));

    private ComponentTheme(IReadOnlyDictionary<string, Entry> values)
    {
        _values = values;
    }

    public ComponentTheme<TControl> Set<TValue>(
        Expression<Func<TControl, TValue>> property,
        TValue value)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.Body is not MemberExpression member)
            throw new ArgumentException("A direct property expression is required.", nameof(property));

        var values = new Dictionary<string, Entry>(_values)
        {
            [member.Member.Name] = new Entry(typeof(TValue), value),
        };
        return new ComponentTheme<TControl>(
            new ReadOnlyDictionary<string, Entry>(values));
    }

    bool IComponentTheme.TryGet(
        string propertyName,
        Type valueType,
        out object? value)
    {
        if (_values.TryGetValue(propertyName, out var entry) &&
            valueType.IsAssignableFrom(entry.ValueType))
        {
            value = entry.Value;
            return true;
        }
        value = null;
        return false;
    }

    IEnumerable<KeyValuePair<string, object?>> IComponentTheme.Values =>
        _values.Select(pair =>
            new KeyValuePair<string, object?>(pair.Key, pair.Value.Value));

    IEnumerable<ComponentThemeEntry> IComponentTheme.Entries =>
        _values.Select(pair =>
            new ComponentThemeEntry(pair.Key, pair.Value.ValueType, pair.Value.Value));

    private sealed record Entry(Type ValueType, object? Value);
}
