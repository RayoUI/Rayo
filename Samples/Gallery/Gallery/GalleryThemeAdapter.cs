namespace Gallery;

using System.Collections;
using System.Reflection;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

/// <summary>
/// Keeps legacy Gallery samples readable in every theme without changing
/// deliberately arbitrary colors used by graphics and color demonstrations.
/// </summary>
internal sealed class GalleryPageHost : Frame
{
    private static readonly HashSet<string> ColorSamplePages =
    [
        "Brushes",
        "ColorPicker",
        "Shadow",
        "Shapes",
        "Styles",
        "Themes",
    ];

    private readonly Func<string, VisualElement> _pageFactory;
    private readonly List<ColorBinding> _colorBindings = [];
    private string _page = string.Empty;
    private bool _isReady;

    public GalleryPageHost(Func<string, VisualElement> pageFactory)
    {
        _pageFactory = pageFactory;
        Background = Color.Transparent;
        BorderThickness = 0;
        Padding = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _isReady = true;
    }

    public void Show(string page)
    {
        _page = page;
        Content = _pageFactory(page);
        CaptureThemeBindings();
        ApplyThemeBindings(RayoThemes.Current);
    }

    protected override void OnThemeApplied(Theme theme)
    {
        base.OnThemeApplied(theme);
        if (_isReady)
        {
            // Components build their visual subtree when mounted, after Show()
            // has assigned the page. Re-scan here so page-local labels and
            // frames participate in every runtime theme change.
            CaptureThemeBindings();
            ApplyThemeBindings(theme);
        }
    }

    private void CaptureThemeBindings()
    {
        _colorBindings.Clear();
        if (Content == null)
            return;

        bool adaptAccents = !ColorSamplePages.Contains(_page);
        CaptureElement(Content, GalleryColorRole.Unknown, adaptAccents);
    }

    private void CaptureElement(VisualElement element, GalleryColorRole inheritedContext, bool adaptAccents)
    {
        var properties = element.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.CanRead &&
                property.CanWrite &&
                property.SetMethod?.IsPublic == true &&
                property.GetIndexParameters().Length == 0 &&
                (property.PropertyType == typeof(Color) ||
                 typeof(Brush).IsAssignableFrom(property.PropertyType)))
            .ToArray();

        GalleryColorRole context = ResolveElementContext(element, properties, inheritedContext, adaptAccents);

        foreach (var property in properties)
        {
            if (!TryReadColor(element, property, out var source))
                continue;

            var role = ResolveRole(element, property.Name, source, context, adaptAccents);
            if (role == GalleryColorRole.Unknown)
                continue;

            _colorBindings.Add(new ColorBinding(element, property, role, source.A));
        }

        foreach (var child in GetChildren(element))
            CaptureElement(child, context, adaptAccents);
    }

    private static GalleryColorRole ResolveElementContext(
        VisualElement element,
        IEnumerable<PropertyInfo> properties,
        GalleryColorRole inheritedContext,
        bool adaptAccents)
    {
        var background = properties.FirstOrDefault(property => property.Name == nameof(Background));
        if (background == null || !TryReadColor(element, background, out var color) || color.A <= 0.001f)
            return inheritedContext;

        var role = ResolveKnownRole(color, adaptAccents);
        return role == GalleryColorRole.Unknown ? GalleryColorRole.Custom : role;
    }

    private static GalleryColorRole ResolveRole(
        VisualElement element,
        string propertyName,
        Color color,
        GalleryColorRole context,
        bool adaptAccents)
    {
        bool isForeground = IsForegroundProperty(propertyName);
        if (isForeground && IsMutedProperty(propertyName))
            return GalleryColorRole.OnDisabled;

        if (isForeground && IsPaletteForeground(color))
        {
            var contextualRole = ToForegroundRole(context);
            return contextualRole != GalleryColorRole.Unknown
                ? contextualRole
                : GalleryColorRole.OnSurface;
        }

        var knownRole = ResolveKnownRole(color, adaptAccents);
        if (knownRole != GalleryColorRole.Unknown)
            return knownRole;

        if (!isForeground)
            return GalleryColorRole.Unknown;

        if (!IsWhiteOrBlack(color))
            return GalleryColorRole.Unknown;

        if (propertyName.StartsWith("Selected", StringComparison.Ordinal))
        {
            var selectedRole = ResolveCompanionRole(element, "SelectedColor", adaptAccents);
            if (selectedRole != GalleryColorRole.Unknown)
                return ToForegroundRole(selectedRole);
        }

        if (propertyName.StartsWith("Header", StringComparison.Ordinal))
        {
            var headerRole = ResolveCompanionRole(element, "HeaderColor", adaptAccents);
            if (headerRole != GalleryColorRole.Unknown)
                return ToForegroundRole(headerRole);
        }

        return ToForegroundRole(context);
    }

    private static GalleryColorRole ResolveCompanionRole(VisualElement element, string propertyName, bool adaptAccents)
    {
        var property = element.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property != null && TryReadColor(element, property, out var color)
            ? ResolveKnownRole(color, adaptAccents)
            : GalleryColorRole.Unknown;
    }

    private static GalleryColorRole ResolveKnownRole(Color color, bool adaptAccents)
    {
        if (adaptAccents)
        {
            if (MatchesEitherPalette(color, colors => colors.Primary)) return GalleryColorRole.Primary;
            if (MatchesEitherPalette(color, colors => colors.PrimaryHover)) return GalleryColorRole.PrimaryHover;
            if (MatchesEitherPalette(color, colors => colors.PrimaryPressed)) return GalleryColorRole.PrimaryPressed;
            if (MatchesEitherPalette(color, colors => colors.Success)) return GalleryColorRole.Success;
            if (MatchesEitherPalette(color, colors => colors.Warning)) return GalleryColorRole.Warning;
            if (MatchesEitherPalette(color, colors => colors.Danger)) return GalleryColorRole.Danger;
            if (MatchesEitherPalette(color, colors => colors.Info)) return GalleryColorRole.Info;
        }

        if (MatchesEitherPalette(color, colors => colors.Background)) return GalleryColorRole.Background;
        if (MatchesEitherPalette(color, colors => colors.Surface)) return GalleryColorRole.Surface;
        if (MatchesEitherPalette(color, colors => colors.SurfaceHover)) return GalleryColorRole.SurfaceHover;
        if (MatchesEitherPalette(color, colors => colors.Border)) return GalleryColorRole.Border;
        if (MatchesEitherPalette(color, colors => colors.OnDisabled)) return GalleryColorRole.OnDisabled;

        var (r, g, b, _) = ToRoundedBytes(color);
        int rgb = (r << 16) | (g << 8) | b;

        if (adaptAccents)
        {
            switch (rgb)
            {
                case 0x3B82F6: return GalleryColorRole.Primary;
                case 0x2563EB: return GalleryColorRole.PrimaryHover;
                case 0x1D4ED8: return GalleryColorRole.PrimaryPressed;
                case 0x22C55E:
                case 0x10B981:
                case 0x4CAF50:
                    return GalleryColorRole.Success;
                case 0xEAB308:
                case 0xF59E0B:
                    return GalleryColorRole.Warning;
                case 0xEF4444:
                case 0xF44336:
                case 0xDC3250:
                    return GalleryColorRole.Danger;
                case 0xA855F7:
                case 0x8B5CF6:
                    return GalleryColorRole.Info;
            }
        }

        return rgb switch
        {
            0x111827 or 0x141418 or 0x191C23 => GalleryColorRole.Background,

            0x1E1E23 or 0x1E212A or 0x1F232C or 0x232328 or 0x232630 or
            0x232632 or 0x262934 or 0x282830 or 0x282832 or 0x2D2D30 or
            0x2D303A or 0x323441 => GalleryColorRole.Surface,

            0x28282D or 0x292D38 or 0x2D3748 or 0x32323C or 0x374151 or
            0x3C3C46 => GalleryColorRole.SurfaceHover,

            0x3C3F49 or 0x3C3F4E or 0x464B5A or 0x4C5260 => GalleryColorRole.Border,

            0x646464 or 0x646978 or 0x787D8C or 0x8C91A0 or 0x969696 or
            0xA0A5B4 or 0xB0B8C8 or 0xB4B4B4 or 0xB4B9C3 or 0xBEC6D6 or
            0xC6CFDE or 0xC8C8C8 => GalleryColorRole.OnDisabled,

            _ => GalleryColorRole.Unknown,
        };
    }

    private void ApplyThemeBindings(Theme theme)
    {
        foreach (var binding in _colorBindings)
        {
            var color = ResolveThemeColor(theme.Colors, binding.Role).WithAlpha(binding.Alpha);
            object value = binding.Property.PropertyType == typeof(Color)
                ? color
                : new SolidColorBrush(color);

            binding.Property.SetValue(binding.Element, value);
        }
    }

    private static Color ResolveThemeColor(ColorPalette palette, GalleryColorRole role) => role switch
    {
        GalleryColorRole.Primary => palette.Primary,
        GalleryColorRole.PrimaryHover => palette.PrimaryHover,
        GalleryColorRole.PrimaryPressed => palette.PrimaryPressed,
        GalleryColorRole.Success => palette.Success,
        GalleryColorRole.Warning => palette.Warning,
        GalleryColorRole.Danger => palette.Danger,
        GalleryColorRole.Info => palette.Info,
        GalleryColorRole.Background => palette.Background,
        GalleryColorRole.Surface => palette.Surface,
        GalleryColorRole.SurfaceHover => palette.SurfaceHover,
        GalleryColorRole.Border => palette.Border,
        GalleryColorRole.OnBackground => palette.OnBackground,
        GalleryColorRole.OnSurface => palette.OnSurface,
        GalleryColorRole.OnPrimary => palette.OnPrimary,
        GalleryColorRole.OnSuccess => palette.OnSuccess,
        GalleryColorRole.OnWarning => palette.OnWarning,
        GalleryColorRole.OnDanger => palette.OnDanger,
        GalleryColorRole.OnInfo => palette.OnInfo,
        GalleryColorRole.OnDisabled => palette.OnDisabled,
        _ => Color.Transparent,
    };

    private static GalleryColorRole ToForegroundRole(GalleryColorRole role) => role switch
    {
        GalleryColorRole.Primary or GalleryColorRole.PrimaryHover or GalleryColorRole.PrimaryPressed =>
            GalleryColorRole.OnPrimary,
        GalleryColorRole.Success => GalleryColorRole.OnSuccess,
        GalleryColorRole.Warning => GalleryColorRole.OnWarning,
        GalleryColorRole.Danger => GalleryColorRole.OnDanger,
        GalleryColorRole.Info => GalleryColorRole.OnInfo,
        GalleryColorRole.Background => GalleryColorRole.OnBackground,
        GalleryColorRole.Surface or GalleryColorRole.SurfaceHover => GalleryColorRole.OnSurface,
        _ => GalleryColorRole.Unknown,
    };

    private static bool IsForegroundProperty(string propertyName) =>
        propertyName.Contains("Foreground", StringComparison.Ordinal) ||
        propertyName.Contains("TextColor", StringComparison.Ordinal) ||
        propertyName.Contains("IconColor", StringComparison.Ordinal) ||
        propertyName is "Color";

    private static bool IsMutedProperty(string propertyName) =>
        propertyName.Contains("Muted", StringComparison.Ordinal) ||
        propertyName.Contains("Disabled", StringComparison.Ordinal) ||
        propertyName.Contains("Placeholder", StringComparison.Ordinal);

    private static bool IsWhiteOrBlack(Color color)
    {
        var (r, g, b, _) = ToRoundedBytes(color);
        return (r == 255 && g == 255 && b == 255) || (r == 0 && g == 0 && b == 0);
    }

    private static bool IsPaletteForeground(Color color) =>
        MatchesEitherPalette(color, colors => colors.OnBackground) ||
        MatchesEitherPalette(color, colors => colors.OnSurface) ||
        MatchesEitherPalette(color, colors => colors.OnPrimary) ||
        MatchesEitherPalette(color, colors => colors.OnSecondary) ||
        MatchesEitherPalette(color, colors => colors.OnSuccess) ||
        MatchesEitherPalette(color, colors => colors.OnWarning) ||
        MatchesEitherPalette(color, colors => colors.OnDanger) ||
        MatchesEitherPalette(color, colors => colors.OnInfo);

    private static bool MatchesEitherPalette(Color color, Func<ColorPalette, Color> selector) =>
        MatchesRgb(color, selector(ColorPalettes.Light)) ||
        MatchesRgb(color, selector(ColorPalettes.Dark));

    private static bool MatchesRgb(Color left, Color right)
    {
        var (leftR, leftG, leftB, _) = ToRoundedBytes(left);
        var (rightR, rightG, rightB, _) = ToRoundedBytes(right);
        return leftR == rightR && leftG == rightG && leftB == rightB;
    }

    private static bool TryReadColor(object element, PropertyInfo property, out Color color)
    {
        try
        {
            object? value = property.GetValue(element);
            if (value is Color directColor)
            {
                color = directColor;
                return true;
            }

            if (value is SolidColorBrush brush)
            {
                color = brush.PrimaryColor;
                return true;
            }
        }
        catch
        {
            // Some computed properties are not readable until their control is mounted.
        }

        color = default;
        return false;
    }

    private static IEnumerable<VisualElement> GetChildren(VisualElement element)
    {
        var method = typeof(VisualElement).GetMethod(
            "GetChildren",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return method?.Invoke(element, null) is IEnumerable children
            ? children.Cast<VisualElement>().ToArray()
            : [];
    }

    private static (int R, int G, int B, int A) ToRoundedBytes(Color color) =>
        ((int)MathF.Round(color.R * 255f),
         (int)MathF.Round(color.G * 255f),
         (int)MathF.Round(color.B * 255f),
         (int)MathF.Round(color.A * 255f));

    private readonly record struct ColorBinding(
        VisualElement Element,
        PropertyInfo Property,
        GalleryColorRole Role,
        float Alpha);

    private enum GalleryColorRole
    {
        Unknown,
        Custom,
        Primary,
        PrimaryHover,
        PrimaryPressed,
        Success,
        Warning,
        Danger,
        Info,
        Background,
        Surface,
        SurfaceHover,
        Border,
        OnBackground,
        OnSurface,
        OnPrimary,
        OnSuccess,
        OnWarning,
        OnDanger,
        OnInfo,
        OnDisabled,
    }
}
