# Theme system

Rayo themes are immutable `ThemeData` values. A theme contains semantic colors,
typography, spacing, shapes, elevation, motion, density, component defaults,
typed custom tokens and third-party extensions.

## Creating a theme

Start from a complete built-in theme and use record `with` expressions:

```csharp
var brandTheme = (RayoThemes.Dark with
    {
        Name = "brand",
        Colors = RayoThemes.Dark.Colors with
        {
            Primary = new Color(236, 72, 153),
            PrimaryHover = new Color(219, 39, 119),
            PrimaryPressed = new Color(190, 24, 93),
        },
        Shapes = RayoThemes.Dark.Shapes with
        {
            Medium = new CornerRadius(8),
        },
    })
    .RebuildComponentDefaults();

app.UseTheme(brandTheme);
```

When changing typography, spacing, shapes, density, colors or preferences,
call `RebuildComponentDefaults()` before applying custom component overrides.
Component defaults deliberately remain immutable; a record `with` expression
does not mutate or silently regenerate them.

`RayoThemes.Light`, `RayoThemes.Dark` and `RayoThemes.HighContrast` are
immutable and complete. Calling
`UseTheme` reapplies themed properties throughout the mounted visual tree while
preserving explicit values assigned by the consumer.

## Theme scopes

`ThemeScope` changes the effective theme only for its content:

```csharp
var preview = new ThemeScope(
    brandTheme,
    new VStack().Children(
        new Label("Brand preview"),
        new Button().Text("Action")));
```

Scopes can be nested. `VisualElement.EffectiveTheme` resolves the nearest
`ThemeScope`, then `UIApplication.ActiveTheme`, then `RayoThemes.Light`.
Popups, menus, drawers, pickers and tooltips capture the effective theme of the
element that opens them even though they render in a detached overlay layer.

## Component customization

Buttons have a complete typed theme:

```csharp
var compact = RayoThemes.Light with
{
    Components = RayoThemes.Light.Components with
    {
        Buttons = RayoThemes.Light.Buttons with
        {
            Padding = new Thickness(8, 4),
            Radius = new CornerRadius(2),
        },
    },
};
```

Every built-in or third-party control can also receive compile-time checked
property defaults:

```csharp
var inputs = ComponentTheme<TextBox>.Empty
    .Set(input => input.FontSize, 16f)
    .Set(input => input.BorderRadius, new CornerRadius(10));

var theme = RayoThemes.Light.WithComponentTheme(inputs);
```

Component defaults participate in the normal cascade and never replace explicit
property values.

## Typed custom tokens

```csharp
static readonly ThemeKey<Color> Accent = new("color.accent");
static readonly ThemeKey<Color> AccentMuted = new("color.accent-muted");

var theme = RayoThemes.Dark
    .WithToken(Accent, new Color(80, 120, 220))
    .WithComputedToken(
        AccentMuted,
        tokens => tokens.Get(Accent).WithAlpha(0.6f));

Color muted = theme.GetToken(AccentMuted);
```

Keys are typed, missing or mismatched values produce descriptive exceptions,
and circular computed-token references report the complete dependency path.
Styles can resolve a token from each matching element's effective theme:

```csharp
new Style<Button>(".accent")
    .Set(Accent, (button, color) => button.Background = color);
```

## States and accessibility

`StateMap<T>` resolves combinations of `Hovered`, `Pressed`, `Focused`,
`Disabled`, `Selected`, `Checked` and `Error`. Exact combinations win; subset
fallback uses the documented priority:

```text
disabled > error > pressed > selected/checked > hovered > focused > normal
```

`ThemePreferences` carries high-contrast, reduced-motion and text-scale
preferences. `ThemeColorUtilities` provides WCAG contrast checks and OKLab
lightness adjustment for perceptual color variants.

Applications can use `ThemeMode.Light`, `Dark` or `System`:

```csharp
app.UseThemeMode(ThemeMode.System);
```

Desktop and Android hosts propagate color scheme, high contrast, reduced
motion, text scale and density changes at runtime. Custom hosts can provide the
same information explicitly:

```csharp
app.UseSystemPreferences(new HostThemePreferences
{
    PrefersDark = true,
    ReduceMotion = true,
    TextScale = 1.25f,
    Density = ThemeDensity.Touch,
});
```

## JSON and hot reload

JSON themes use a versioned schema, support `basedOn`, typed tokens, component
overrides and registered third-party extensions. Extension identifiers are
provided through an explicit registry:

```csharp
var registry = new ThemeJsonRegistry()
    .Register<ChartTheme>("chart");

var json = ThemeJson.Serialize(theme, registry: registry);
var restored = ThemeJson.Deserialize(json, registry: registry);
```

Hot reload retains the last valid theme and applies successful updates on the
UI thread:

```csharp
app.WatchThemeFile(
    "theme.json",
    onLoadFailed: error => Console.Error.WriteLine(error),
    registry: registry);
```

The DevTool Theme tab reads the effective theme from the connected process. If
an element is selected, the inspector shows that element's scoped theme.

## Resolution order

The intended precedence is:

1. Local values and bindings.
2. Matching stylesheet declarations.
3. Component-theme defaults.
4. Semantic values from the effective `ThemeData`.
5. Defensive control defaults.

Controls implement `OnThemeApplied(ThemeData)` and call `SetThemeValue` for
theme-managed properties. `UseThemeDefaults()` on supported controls clears
their local overrides and reapplies the effective theme.
