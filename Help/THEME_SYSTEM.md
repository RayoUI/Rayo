# Theme System

This guide describes Rayo's current `Theme` system: how themes are defined, how
controls consume them, how themes change at runtime, and how they interact with
`StyleTokens` and the operating system color scheme.

## Contents

1. [System model](#system-model)
2. [Quick start](#quick-start)
3. [Semantic color palette](#semantic-color-palette)
4. [Typed button tokens](#typed-button-tokens)
5. [Creating a theme](#creating-a-theme)
6. [Applying and reading the active theme](#applying-and-reading-the-active-theme)
7. [Theme change propagation](#theme-change-propagation)
8. [Explicit values and theme values](#explicit-values-and-theme-values)
9. [Integrating custom controls](#integrating-custom-controls)
10. [StyleTokens integration](#styletokens-integration)
11. [Theme and the operating system color scheme](#theme-and-the-operating-system-color-scheme)
12. [Quick API reference](#quick-api-reference)
13. [Current considerations](#current-considerations)

## System model

The system has four layers:

| Layer | Type | Responsibility |
|---|---|---|
| Global theme | `Theme` | Groups a name, a palette, button tokens, and free-form overrides |
| Semantic colors | `ColorPalette` | Defines roles such as `Primary`, `Surface`, and `Danger`, including their contrast colors |
| Control tokens | `ButtonTheme` | Defines the visual states of each button variant |
| Free-form tokens | `Theme.Set<T>()` + `StyleTokens` | Lets an application define custom named tokens |

Rayo includes two ready-to-use themes:

- `RayoThemes.Light`
- `RayoThemes.Dark`

The initial `UIApplication` theme is `RayoThemes.Light`.

## Quick start

```csharp
using Rayo.Core;
using Rayo.Styling;

var app = new UIApplication("My application", 1280, 720)
    .UseTheme(RayoThemes.Dark);

app.Run();
```

To change it at runtime:

```csharp
RayoThemes.UseTheme(RayoThemes.Light);
```

`RayoThemes.UseTheme()` is the most portable entry point. When a
`UIApplication` exists, it delegates to `UIApplication.UseTheme()`. On mobile
hosts where a `UIApplication` may not exist, it updates the active `UITree`
directly.

## Semantic color palette

`ColorPalette` is an immutable `record`. Controls consume roles instead of
locally meaningful colors, allowing the entire visual identity to be replaced
without rewriting each control.

| Group | Roles |
|---|---|
| Primary | `Primary`, `PrimaryHover`, `PrimaryPressed`, `OnPrimary` |
| Secondary | `Secondary`, `SecondaryHover`, `SecondaryPressed`, `OnSecondary` |
| Background | `Background`, `OnBackground` |
| Surface | `Surface`, `SurfaceHover`, `SurfacePressed`, `OnSurface` |
| General state | `Border`, `Focus`, `Disabled`, `OnDisabled` |
| Semantic state | `Success`/`OnSuccess`, `Warning`/`OnWarning`, `Danger`/`OnDanger`, `Info`/`OnInfo` |

The `On...` prefix represents the content color that should be drawn over the
corresponding role. For example, text placed over `Primary` should use
`OnPrimary`.

The built-in palettes are `ColorPalettes.Light` and `ColorPalettes.Dark`.
Because `ColorPalette` is a `record`, the usual way to customize a palette is to
derive it from an existing one:

```csharp
var brandPalette = ColorPalettes.Dark with
{
    Primary = new Color(236, 72, 153),
    PrimaryHover = new Color(219, 39, 119),
    PrimaryPressed = new Color(190, 24, 93),
    OnPrimary = Color.White,
    Focus = new Color(244, 114, 182),
};
```

Every `ColorPalette` property is `required`. Starting from an existing palette
avoids leaving any role undefined.

## Typed button tokens

`ButtonTheme` contains four variants:

- `Primary`
- `Secondary`
- `Danger`
- `Ghost`

Each variant is a `ButtonColors` value containing `Background`,
`HoverBackground`, `PressedBackground`, `Foreground`, and `Border`.

When a `Theme` is created with only a palette, Rayo generates these tokens with
`ButtonTheme.FromPalette()`. They can also be customized independently:

```csharp
var buttons = ButtonTheme.FromPalette(brandPalette);

buttons = buttons with
{
    Ghost = buttons.Ghost with
    {
        Foreground = brandPalette.Primary,
        Border = brandPalette.Primary.WithAlpha(0.35f),
    },
};

var brandTheme = new Theme("brand", brandPalette, buttons);
```

Changing the palette later with `theme.UseColors(palette)` regenerates
`ButtonTheme` from that palette. Apply `UseButtons()` afterwards when button
overrides are required.

## Creating a theme

The constructor without a palette uses the light palette:

```csharp
var theme = new Theme("custom");
```

For a complete visual identity:

```csharp
var theme = new Theme("brand", brandPalette)
    .Set("--panel-radius", 12f)
    .Set("--hero-spacing", 24f);
```

The available fluent API is:

```csharp
theme
    .UseColors(brandPalette)
    .UseButtons(buttons)
    .Set("--app-token", value);
```

`Theme.Get<T>()`, `TryGet<T>()`, and `Contains()` read free-form tokens. The
theme name must not be empty.

## Applying and reading the active theme

With a desktop application:

```csharp
UIApplication.Current?.UseTheme(theme);

Theme current = UIApplication.Current?.ActiveTheme
    ?? RayoThemes.Current;
```

From code shared across hosts:

```csharp
RayoThemes.UseTheme(theme);
Theme current = RayoThemes.Current;
```

`UIApplication.ActiveTheme` is never null and starts as `RayoThemes.Light`.
`RayoThemes.Current` returns the application's `ActiveTheme` when an application
exists; otherwise, it returns the last theme set through `RayoThemes`.

To observe global changes:

```csharp
UIApplication.ThemeChanged += OnThemeChanged;

static void OnThemeChanged(Theme theme)
{
    Console.WriteLine($"Active theme: {theme.Name}");
}
```

The event is static. Its owner should unsubscribe when the subscription is no
longer needed to avoid retaining references accidentally.

## Theme change propagation

Calling `UseTheme()` performs the following operations:

1. Updates `UIApplication.ActiveTheme` and `RayoThemes.Current`.
2. Raises `UIApplication.ThemeChanged`.
3. Walks the visual root and its descendants.
4. Updates both application overlays and `UITree` overlays.
5. Calls `OnThemeApplied(theme)` on each `VisualElement`.
6. Makes built `Component` instances reapply global and local styles and request
   a repaint.

Tree propagation processes children before their composite control. This gives
the composite control the final word over its implementation details while
preserving explicit customizations on public children.

Built-in controls do not require a tree rebuild when the theme changes.

## Explicit values and theme values

Controls apply defaults through `SetThemeValue()`. When the application
explicitly assigns a theme-managed property, that property stops following
subsequent global theme changes:

```csharp
var button = new Button()
    .Variant(ButtonVariant.Primary)
    .Background(new Color(20, 90, 180)); // Application override
```

After a theme change, the button's other theme properties may update, while
`Background` keeps the explicitly assigned value.

`Button` and `ButtonIcon` expose `UseThemeDefaults()` to remove their color
overrides and immediately reapply the active theme:

```csharp
button.UseThemeDefaults();
```

The `Button(ColorPalette)` constructor and `ApplyPalette()` produce explicit
colors. By design, those colors also survive global theme changes.

## Integrating custom controls

A control participates in the theme system by overriding `OnThemeApplied()` and
applying its defaults with `SetThemeValue()`:

```csharp
public sealed class BrandCard : Card
{
    protected override void OnThemeApplied(Theme theme)
    {
        base.OnThemeApplied(theme);

        SetThemeValue(
            nameof(BorderBrush),
            (Brush)theme.Colors.Primary,
            value => BorderBrush = value);
    }
}
```

Important rules:

- Call `base.OnThemeApplied(theme)` when extending an already themed control.
- Use `SetThemeValue()` for properties that must respect consumer overrides.
- A control built directly on `VisualElement` must call `InitializeTheme()` once
  its required properties are ready.
- To expose a public reset operation, wrap `ResetThemeValues()` in a control
  method such as `UseThemeDefaults()`.
- Use `RayoThemes.Current` as the current theme source in code that supports
  hosts without a `UIApplication`.

Many built-in controls currently override `OnThemeApplied()`, including buttons,
text fields, labels, selection controls, navigation, menus, pickers, lists,
tables, progress indicators, loading indicators, cards, and overlays.

## StyleTokens integration

`Theme` can also override free-form tokens consumed by `StyleTokens`.
`StyleTokens.Get<T>(name)` uses the following resolution order:

1. Token from `UIApplication.ActiveTheme`.
2. Computed `StyleTokens` factory.
3. Concrete `StyleTokens` value.

```csharp
var tokens = new StyleTokens()
    .Set("--panel", ColorPalettes.Light.Surface)
    .Set("--radius", 8f);

var dark = new Theme("dark", ColorPalettes.Dark)
    .Set("--panel", ColorPalettes.Dark.Surface);
```

A `StyleSheet` stores actions and is normally built only once. To resolve a
token again when a `Component` reapplies styles after a theme change, read it
inside `Set()`:

```csharp
protected override StyleSheet? BuildStyles() =>
[
    new Style<Frame>().Set(frame =>
        frame.Background = tokens.Get<Color>("--panel")),
];
```

This form resolves the token every time the rule is applied. In contrast:

```csharp
new Style<Frame>().Background(tokens.Get<Color>("--panel"))
```

resolves the value while building the `StyleSheet` and retains that value in the
rule.

Theme overrides in `StyleTokens` are read from
`UIApplication.Current.ActiveTheme`. On a host without a `UIApplication`,
built-in controls still follow `RayoThemes.Current`, but `StyleTokens` uses its
own values or factories.

## Theme and the operating system color scheme

`Theme` and `ColorScheme` are independent mechanisms:

- `Theme` is an application visual identity decision.
- `ColorSchemeHelper` detects the operating system `Light`/`Dark` scheme and
  activates `Style.When(ColorScheme, ...)` blocks.

Changing the system scheme does not automatically select `RayoThemes.Light` or
`RayoThemes.Dark`, and applying a `Theme` does not change
`ColorSchemeHelper.Current`. An application that wants to follow the system must
translate the change explicitly:

```csharp
ColorSchemeHelper.ColorSchemeChanged += scheme =>
    RayoThemes.UseTheme(
        scheme == ColorScheme.Dark
            ? RayoThemes.Dark
            : RayoThemes.Light);
```

## Quick API reference

| API | Function |
|---|---|
| `new Theme(name)` | Creates a theme with the light palette |
| `new Theme(name, colors, buttons?)` | Creates a theme with a palette and optional button tokens |
| `Theme.Colors` | Semantic palette active in the theme |
| `Theme.Buttons` | Typed tokens for button variants |
| `Theme.UseColors(colors)` | Replaces the palette and regenerates button tokens |
| `Theme.UseButtons(buttons)` | Replaces the button tokens |
| `Theme.Set<T>(name, value)` | Defines a free-form token |
| `Theme.Get<T>(name, fallback)` | Reads a token or returns the fallback |
| `Theme.TryGet<T>(name, out value)` | Attempts to read a typed token |
| `Theme.Contains(name)` | Checks whether the theme contains a token |
| `RayoThemes.Light` / `Dark` | Built-in themes |
| `RayoThemes.Current` | Current global theme, with or without `UIApplication` |
| `RayoThemes.UseTheme(theme)` | Portable entry point for changing the theme |
| `UIApplication.ActiveTheme` | Application theme; initially `Light` |
| `UIApplication.UseTheme(theme)` | Changes and propagates the theme |
| `UIApplication.ThemeChanged` | Static event raised after a change |

## Current considerations

- `Theme` is mutable. Calling `Set()`, `UseColors()`, or `UseButtons()` does not
  raise a notification by itself. If the instance is already active, call
  `UseTheme(theme)` again to propagate its new values.
- `UseColors()` regenerates every button token and replaces any previous
  `ButtonTheme` customization.
- Free-form tokens are identified by `string`. A read with the wrong type does
  not match the theme override and may fall back to the `StyleTokens` value.
- Styles whose values are resolved during `BuildStyles()` retain those values.
  Use a setter that reads the token when the rule is applied for dynamic
  resolution.
- Automatic override tracking only covers properties the control previously
  registered through `SetThemeValue()`.

