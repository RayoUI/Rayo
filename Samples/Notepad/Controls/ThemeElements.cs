using Rayo.Controls;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

namespace Notepad.Controls;

internal sealed class ThemeFrame : Frame
{
    private readonly Func<ColorPalette, Color> _backgroundSelector;
    private readonly Func<ColorPalette, Color>? _borderSelector;

    public ThemeFrame(
        Func<ColorPalette, Color> backgroundSelector,
        Func<ColorPalette, Color>? borderSelector = null)
    {
        _backgroundSelector = backgroundSelector;
        _borderSelector = borderSelector;
        InitializeTheme();
    }

    protected override void OnThemeApplied(Theme theme)
    {
        SetThemeValue(
            nameof(Background),
            (Brush)_backgroundSelector(theme.Colors),
            value => Background = value);

        if (_borderSelector != null)
        {
            SetThemeValue(
                nameof(BorderBrush),
                (Brush)_borderSelector(theme.Colors),
                value => BorderBrush = value);
        }
    }
}

internal sealed class ThemeLabel : Label
{
    private readonly Func<ColorPalette, Color> _foregroundSelector;

    public ThemeLabel(Func<ColorPalette, Color> foregroundSelector)
    {
        _foregroundSelector = foregroundSelector;
        ResetThemeValues();
    }

    protected override void OnThemeApplied(Theme theme)
    {
        if (_foregroundSelector == null)
            return;

        SetThemeValue(
            nameof(Foreground),
            (Brush)_foregroundSelector(theme.Colors),
            value => Foreground = value);
    }
}
