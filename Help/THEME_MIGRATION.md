# Theme migration

The new theme system is intentionally a breaking change.

| Previous API | Replacement |
|---|---|
| `Theme` | `ThemeData` |
| `ColorPalette` | `ColorScheme` |
| `ColorPalettes.Light/Dark` | `ColorSchemes.Light/Dark` |
| `RayoThemes.Current` | `VisualElement.EffectiveTheme` or `UIApplication.ActiveTheme` |
| `StyleTokens` | `ThemeTokenSet` with `ThemeKey<T>` |
| `new Theme(name, palette)` | `new ThemeData(name, scheme, brightness: ...)` |
| `theme.Set("--key", value)` | `theme.WithToken(new ThemeKey<T>("key"), value)` |
| RGB channel shading | `ThemeColorUtilities.AdjustLightness` |

Custom controls should change:

```csharp
protected override void OnThemeApplied(ThemeData theme)
{
    SetThemeValue(
        nameof(Background),
        (Brush)theme.Colors.Surface,
        value => Background = value);
}
```

Inside a `VisualElement`, use `EffectiveTheme`; do not read an application
singleton. When adding detached UI, pass its owner:

```csharp
OverlayManager.AddOverlay(popup, owner);
```
