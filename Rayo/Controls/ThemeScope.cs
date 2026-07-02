namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Rendering;
using Rayo.Styling;

/// <summary>
/// Applies a theme to its content subtree without changing the application theme.
/// </summary>
public sealed class ThemeScope : ContentView<ThemeScope>
{
    private ThemeData _theme;

    public ThemeData Theme
    {
        get => _theme;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_theme, value))
                return;
            var previous = _theme;
            _theme = value;
            NotifyThemeChanged(value);
            if (value.RequiresMeasureComparedTo(previous))
                InvalidateMeasure();
            else
                MarkNeedsPaint();
        }
    }

    internal override ThemeData? ScopedTheme => Theme;

    public ThemeScope(ThemeData theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public ThemeScope(ThemeData theme, VisualElement content) : this(theme)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public ThemeScope ContentElement(VisualElement content)
    {
        Content = content;
        return this;
    }

    public override void Render(IRenderer renderer)
    {
        // ThemeScope is intentionally non-visual; the tree renders its content.
    }
}
