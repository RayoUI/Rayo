# Style Engine — Reference

Rayo's CSS-inspired style engine. Lets you declare visual rules applied to element subtrees, with full reactive support: styles are re-applied automatically when the window is resized, element state changes, the OS theme changes, and more.

---

## Table of Contents

1. [Core concepts](#1-core-concepts)
2. [StyleSheet — rule collection](#2-stylesheet--rule-collection)
3. [Selectors](#3-selectors)
4. [Specificity](#4-specificity)
5. [Property setters](#5-property-setters)
6. [Conditions — When()](#6-conditions--when)
   - 6.1 [Element state (StyleTrigger)](#61-element-state-styletrigger)
   - 6.2 [Named breakpoints](#62-named-breakpoints)
   - 6.3 [Predicate breakpoints](#63-predicate-breakpoints)
   - 6.4 [Platform](#64-platform)
   - 6.5 [OS color scheme](#65-os-color-scheme)
   - 6.6 [Orientation](#66-orientation)
   - 6.7 [Screen density](#67-screen-density)
7. [Structural selectors](#7-structural-selectors)
   - 7.1 [ChildOf / DescendantOf](#71-childof--descendantof)
   - 7.2 [SiblingOf / ImmediateSiblingOf](#72-siblingof--immediatesiblingof)
   - 7.3 [FirstChild / LastChild / NthChild](#73-firstchild--lastchild--nthchild)
   - 7.4 [Not()](#74-not)
8. [Composition](#8-composition)
   - 8.1 [Extend](#81-extend)
   - 8.2 [WithTransition](#82-withtransition)
   - 8.3 [Important()](#83-important)
9. [Design tokens](#9-design-tokens)
10. [Theme system](#10-theme-system)
11. [Style scope](#11-style-scope)
12. [Applying styles](#12-applying-styles)
    - 12.1 [Global styles (app-level)](#121-global-styles-app-level)
    - 12.2 [Component styles (UserControl)](#122-component-styles-usercontrol)
    - 12.3 [StyleEngine — manual application](#123-styleengine--manual-application)
    - 12.4 [GetComputedStyle — debugging](#124-getcomputedstyle--debugging)
13. [Application priority](#13-application-priority)
14. [Reactivity — when styles are re-applied](#14-reactivity--when-styles-are-re-applied)
15. [Quick API reference](#15-quick-api-reference)

---

## 1. Core concepts

A style is an instance of `Style<T>` where `T` is the target element type. Each style contains:

- An optional **selector** (id, class, multi-class, or id+class) that filters which elements receive the rule.
- **Base setters** that are always applied when the selector matches.
- **Conditional setters** applied only when a condition is true (state, breakpoint, platform, etc.).

```csharp
// Type selector — targets all Button elements
new Style<Button>()
    .Height(36)
    .BorderRadius(8)

// Class selector — only buttons with class "primary"
new Style<Button>(".primary")
    .Background(new Color(0, 120, 212))

// Id selector — a single element with id "submit"
new Style<Button>("#submit")
    .Background(new Color(0, 180, 80))
```

---

## 2. StyleSheet — rule collection

`StyleSheet` is an ordered list of `StyleRule` objects. Supports C# 12 collection-expression syntax:

```csharp
StyleSheet sheet =
[
    new Style<Button>().Height(36).BorderRadius(8),
    new Style<Label>().FontSize(14),
];
```

Can also be built imperatively:

```csharp
var sheet = new StyleSheet();
sheet.Add(new Style<Button>().Height(36));
```

---

## 3. Selectors

| Syntax                        | CSS equivalent        | Description                                         |
|-------------------------------|-----------------------|-----------------------------------------------------|
| `new Style<T>()`              | `T { }`               | All elements of type T                              |
| `new Style<T>("#my-id")`      | `#my-id { }`          | Element with `Id == "my-id"` (highest specificity)  |
| `new Style<T>(".primary")`    | `.primary { }`        | Elements that have the class `"primary"`            |
| `new Style<T>(".a.b")`        | `.a.b { }`            | Elements that have **both** classes (AND logic)     |
| `new Style<T>("#my-id.primary")` | `#my-id.primary { }` | Element with id and class (AND logic)               |
| `Style.Id<T>("my-id")`        | `#my-id { }`          | Factory alternative for id selector                 |
| `Style.Class<T>("primary")`   | `.primary { }`        | Factory alternative for class selector              |

**Assigning classes and ids to elements:**

```csharp
new Button().Classes("primary large")   // multiple classes separated by spaces
new Label().Classes("section-title")
new Button().Id("submit")
```

---

## 4. Specificity

Specificity determines application order when rules conflict. Higher specificity wins.

| Selector type              | Specificity |
|----------------------------|-------------|
| Type only (`Style<T>()`)   | 1           |
| Single class (`.primary`)  | 11          |
| Two classes (`.a.b`)       | 21          |
| Three classes (`.a.b.c`)   | 31          |
| Id (`#submit`)             | 101         |
| Id + class (`#submit.primary`) | 111     |

At equal specificity, the rule declared **later** wins (declaration order).

Rules marked `Important()` are applied **after** all normal rules, regardless of their specificity.

---

## 5. Property setters

Style setters are generated automatically for public properties on `VisualElement` descendants. This means built-in controls and custom controls inherit style setter support without requiring a class-level opt-in attribute.

```csharp
new Style<Button>()
    .Height(36)
    .Width(120)
    .BorderRadius(8)
    .Background(new Color(0, 120, 212))
    .TextColor(new Color(255, 255, 255))
    .FontSize(14)
    .Margin(new Thickness(4))
    .Padding(new Thickness(8, 4))
    .HorizontalAlignment(HorizontalAlignment.Stretch)
```

For properties not covered by generated methods, or for custom logic, use `Set()`:

```csharp
new Style<Button>().Set(b => b.IsEnabled = false)
```

Use `[NoReactive]` on a class or property when you intentionally want to exclude it from fluent API and generated setter generation.

---

## 6. Conditions — When()

All `When()` overloads accept an `Action<Style<T>>` lambda where the conditional setters are declared. Conditional setters **accumulate on top of** the base setters rather than replacing them.

### 6.1 Element state (StyleTrigger)

Equivalent to CSS pseudo-classes `:hover`, `:active`, `:focus`, `:disabled`.

States are re-evaluated automatically by `StyleApplier` whenever `IsHovered`, `IsPressed`, or `IsEnabled` changes.

```csharp
new Style<Button>()
    .Background(new Color(0, 120, 212))
    .When(StyleTrigger.Hover,    s => s.Background(new Color(0, 100, 190)))
    .When(StyleTrigger.Pressed,  s => s.Background(new Color(0,  80, 160)))
    .When(StyleTrigger.Focused,  s => s.BorderWidth(2))
    .When(StyleTrigger.Disabled, s => s.Background(new Color(150, 150, 150)))
```

| Value      | Condition                                       |
|------------|-------------------------------------------------|
| `Hover`    | The pointer is over the element                 |
| `Pressed`  | The element is being pressed                    |
| `Focused`  | The element has keyboard focus                  |
| `Disabled` | `IsEnabled == false`                            |

### 6.2 Named breakpoints

Equivalent to CSS `@media (min-width: ...)`. Evaluated against `UIApplication.WindowWidth`.

```csharp
new Style<Label>()
    .FontSize(14)
    .When(Breakpoint.XSmall, s => s.FontSize(12))
    .When(Breakpoint.Small,  s => s.FontSize(13))
    .When(Breakpoint.Medium, s => s.FontSize(14))
    .When(Breakpoint.Large,  s => s.FontSize(16))
    .When(Breakpoint.XLarge, s => s.FontSize(18))
```

| Breakpoint | Window width range |
|------------|--------------------|
| `XSmall`   | < 480 px           |
| `Small`    | 480 – 767 px       |
| `Medium`   | 768 – 1023 px      |
| `Large`    | 1024 – 1439 px     |
| `XLarge`   | ≥ 1440 px          |

Breakpoint styles are re-applied automatically every time the window crosses a threshold. This includes maximize/restore on Windows.

### 6.3 Predicate breakpoints

For exact-width conditions or custom expressions. The lambda receives the current window width in pixels. Re-evaluated on **every size change**, not only at named thresholds.

```csharp
// Named, reusable predicates
static readonly Func<float, bool> Mobile  = w => w < 600;
static readonly Func<float, bool> Tablet  = w => w is >= 600 and < 1024;
static readonly Func<float, bool> Desktop = w => w >= 1024;

new Style<Frame>(".sidebar")
    .Width(240)
    .When(Mobile,  s => s.Width(0))       // hidden on mobile
    .When(Tablet,  s => s.Width(64))      // icon-only on tablet
    .When(Desktop, s => s.Width(240))     // full width on desktop

// Also works with inline lambdas
new Style<Label>()
    .When(w => w >= 900, s => s.FontSize(18))
```

### 6.4 Platform

Evaluated once at startup; the platform never changes at runtime.

```csharp
new Style<Button>()
    .BorderRadius(6)
    .When(PlatformType.Windows, s => s.BorderRadius(4))
    .When(PlatformType.MacOS,   s => s.BorderRadius(10))
    .When(PlatformType.Linux,   s => s.BorderRadius(2))
    .When(PlatformType.Android, s => s.BorderRadius(20).Height(48))
    .When(PlatformType.iOS,     s => s.BorderRadius(14).Height(50))
```

| Value      | Detected platform |
|------------|-------------------|
| `Windows`  | Windows           |
| `MacOS`    | macOS             |
| `Linux`    | Linux             |
| `Android`  | Android           |
| `iOS`      | iOS / iPadOS      |

### 6.5 OS color scheme

Detects whether the OS uses dark or light mode (equivalent to CSS `prefers-color-scheme`). Re-evaluated automatically every ~5 seconds.

**Detection:** Windows — `AppsUseLightTheme` registry key. Other OS — falls back to `Light`.

```csharp
new Style<Label>(".scheme-status")
    .Foreground(new Color(80, 200, 120))
    .Text("Light mode")
    .When(ColorScheme.Dark,  s => s.Text("Dark mode") .Foreground(new Color(100, 140, 255)))
    .When(ColorScheme.Light, s => s.Text("Light mode").Foreground(new Color(255, 160, 80)))
```

| Value   | Condition                              |
|---------|----------------------------------------|
| `Light` | The OS is using a light theme (default) |
| `Dark`  | The OS is using a dark theme           |

### 6.6 Orientation

Portrait when `height > width`. Re-evaluated on every window resize.

```csharp
new Style<HStack>(".toolbar")
    .When(Rayo.Styling.Orientation.Portrait,  s => s.VerticalAlignment(VerticalAlignment.Bottom))
    .When(Rayo.Styling.Orientation.Landscape, s => s.HorizontalAlignment(HorizontalAlignment.Right))
```

> **Note:** `Orientation` exists in two namespaces (`Rayo.Controls` and `Rayo.Styling`). In files that import both, use the fully qualified name: `Rayo.Styling.Orientation.Portrait`.

| Value       | Condition                          |
|-------------|------------------------------------|
| `Portrait`  | Window height > window width       |
| `Landscape` | Window width ≥ window height       |

### 6.7 Screen density

Based on monitor DPI. On Windows, uses Win32 `GetDeviceCaps`. Can be overridden manually with `ScreenDensityHelper.Override(density)`.

```csharp
new Style<Image>()
    .When(ScreenDensity.Normal,    s => s.Source("icon.png"))
    .When(ScreenDensity.High,      s => s.Source("icon@2x.png"))
    .When(ScreenDensity.ExtraHigh, s => s.Source("icon@3x.png"))
```

| Value        | DPI range         |
|--------------|-------------------|
| `Low`        | < 96 dpi          |
| `Normal`     | 96 – 143 dpi      |
| `High`       | 144 – 191 dpi     |
| `ExtraHigh`  | ≥ 192 dpi         |

---

## 7. Structural selectors

### 7.1 ChildOf / DescendantOf

```csharp
// CSS: Panel > Button — only Buttons that are direct children of Panel
new Style<Button>().ChildOf<Panel>()
    .Margin(new Thickness(4))

// CSS: Card Button — Buttons at any nesting level inside Card
new Style<Button>().DescendantOf<Card>()
    .BorderRadius(12)
```

### 7.2 SiblingOf / ImmediateSiblingOf

```csharp
// CSS: Label ~ Button — Buttons that share a parent with any Label
new Style<Button>().SiblingOf<Label>()
    .Margin(new Thickness(left: 8))

// CSS: Label + Button — Button immediately preceded by a Label
new Style<Button>().ImmediateSiblingOf<Label>()
    .Margin(new Thickness(left: 4))
```

### 7.3 FirstChild / LastChild / NthChild

Indices are zero-based internally, but `NthChild` accepts a 1-based step (matching CSS convention).

```csharp
// CSS: .item:first-child
new Style<Label>(".item").FirstChild()
    .FontSize(16)
    .Foreground(new Color(255, 160, 80))

// CSS: .item:last-child
new Style<Label>(".item").LastChild()
    .Foreground(new Color(220, 80, 160))

// CSS: li:nth-child(2) — matches the 2nd, 4th, 6th… child
new Style<Label>(".item").NthChild(2)
    .Background(new Color(50, 50, 62))
```

### 7.4 Not()

```csharp
// CSS: .item:not(.excluded) — all .item elements except those that also have .excluded
new Style<Label>(".item")
    .Not(".excluded")
    .Foreground(new Color(80, 200, 120))

// CSS: :not(#header) — elements without that specific id
new Style<Label>()
    .Not("#header")
    .FontSize(14)

// By type — excludes a specific subtype
new Style<VisualElement>()
    .Not<Button>()
    .Margin(new Thickness(4))
```

---

## 8. Composition

### 8.1 Extend

Copies the base setters from another rule, equivalent to CSS `@extend`. Only unconditional setters are copied.

```csharp
var baseCard = new Style<Frame>()
    .BorderRadius(8)
    .Padding(new Thickness(16));

var primaryCard = new Style<Frame>(".primary")
    .Extend(baseCard)                          // inherits BorderRadius and Padding
    .Background(new Color(0, 120, 212));
```

### 8.2 WithTransition

Records transition metadata that `StyleApplier` uses when re-applying state rules. Enables animated property changes on hover/press.

```csharp
new Style<Button>()
    .Background(new Color(0, 120, 212))
    .When(StyleTrigger.Hover, s => s.Background(new Color(0, 100, 190)))
    .WithTransition(150)                        // 150 ms with Out Quad easing by default

// With custom easing
.WithTransition(200, Rayo.Animation.Easing.InOutCubic)
```

### 8.3 Important()

Marks the rule as `!important`. It is applied **after** all normal rules, even those with higher specificity.

```csharp
// This rule (.danger) has specificity 11,
// but Important() forces it to be applied after .danger.override (specificity 21).
new Style<Label>(".danger")
    .Foreground(new Color(220, 50, 50))
    .Important()

new Style<Label>(".danger.override")
    .Foreground(new Color(80, 200, 120))       // would normally win — but not here
```

---

## 9. Design tokens

`StyleTokens` is a named-value dictionary that lets you define a centralised palette and reference it from multiple rules. When a theme is active, `Get<T>()` checks the theme first before its own dictionary.

```csharp
var tokens = new StyleTokens()
    .Set("--primary",    new Color(0, 120, 212))
    .Set("--surface",    new Color(35, 35, 44))
    .Set("--text",       new Color(230, 230, 240))
    .Set("--radius",     8f)
    .Set("--spacing",    16f);

// Computed tokens — evaluated lazily on each Get(), not at Set() time
tokens
    .Set("--primary-dim",  t => t.Get<Color>("--primary").WithAlpha(0.6f))
    .Set("--large-radius", t => t.Get<float>("--radius") * 2f);
```

**Using tokens in style rules:**

```csharp
StyleSheet sheet =
[
    new Style<Button>()
        .Background(tokens.Get<Color>("--primary"))
        .BorderRadius(tokens.Get<float>("--radius")),

    new Style<Frame>()
        .Background(tokens.Get<Color>("--surface"))
        .Padding(tokens.Get<float>("--spacing")),
];
```

**StyleTokens API:**

| Method                                   | Description                                               |
|------------------------------------------|-----------------------------------------------------------|
| `Set<T>(name, value)`                    | Registers a concrete value                                |
| `Set<T>(name, Func<StyleTokens, T>)`     | Registers a computed (lazy) token, evaluated on each `Get` |
| `Get<T>(name)`                           | Reads the value; throws if missing or wrong type          |
| `Get<T>(name, fallback)`                 | Reads the value, returning `fallback` on any error        |
| `TryGet<T>(name, out T? value)`          | Attempts to read without throwing                         |
| `Contains(name)`                         | Returns `true` if the token exists                        |
| `Remove(name)`                           | Removes a token                                           |

---

## 10. Theme system

A `Theme` is a token override layer. When active, `StyleTokens.Get<T>()` checks the theme before its own dictionary. When the theme changes, all `UserControl` instances automatically re-apply their styles.

```csharp
// Define themes
var darkTheme = new Theme("dark")
    .Set("--surface",  new Color(18, 18, 18))
    .Set("--text",     Color.White)
    .Set("--primary",  new Color(100, 140, 255));

var lightTheme = new Theme("light")
    .Set("--surface",  Color.White)
    .Set("--text",     new Color(20, 20, 20))
    .Set("--primary",  new Color(0, 120, 212));

// Switch theme at runtime
UIApplication.Current?.UseTheme(darkTheme);

// Or set it during initial configuration
var app = new UIApplication("My App", 1280, 720);
app.UseTheme(darkTheme);
app.Run();
```

**Theme-toggle button:**

```csharp
new Button()
    .Text("Dark mode")
    .OnTap(() => UIApplication.Current?.UseTheme(darkTheme))
```

**Subscribing to theme changes:**

```csharp
UIApplication.ThemeChanged += theme =>
{
    Console.WriteLine($"Active theme: {theme.Name}");
};
```

**Theme API:**

| Member                           | Description                                           |
|----------------------------------|-------------------------------------------------------|
| `new Theme(name)`                | Creates a theme with the given name                   |
| `Set<T>(token, value)`           | Registers a value; returns `this` for chaining        |
| `Get<T>(token, fallback)`        | Reads a value from the theme                          |
| `TryGet<T>(token, out T? value)` | Attempts to read without throwing                     |
| `Contains(token)`                | Returns `true` if the theme defines the token         |
| `UIApplication.UseTheme(theme)`  | Activates the theme and notifies all components       |
| `UIApplication.ActiveTheme`      | The currently active theme (may be `null`)            |
| `UIApplication.ThemeChanged`     | Static event fired when the active theme changes      |

---

## 11. Style scope

Controls whether rules declared in a `UserControl` cascade into nested `UserControl` children.

```csharp
public class MyComponent : UserControl
{
    // Default: Global — rules propagate to all descendants
    protected override StyleScope StyleScope => StyleScope.Global;

    // Local: rules stop at nested UserControl boundaries
    protected override StyleScope StyleScope => StyleScope.Local;
}
```

| Value    | Behaviour                                                         | CSS equivalent    |
|----------|-------------------------------------------------------------------|-------------------|
| `Global` | Rules penetrate into all nested `UserControl` subtrees            | Normal cascade    |
| `Local`  | Rules stop when a nested `UserControl` boundary is encountered    | CSS Shadow DOM    |

---

## 12. Applying styles

### 12.1 Global styles (app-level)

Applied to **every** `UserControl` in the tree. Component styles are applied afterwards and therefore take priority at equal specificity.

```csharp
var app = new UIApplication("My App", 1280, 720);
app.UseGlobalStyles(
[
    new Style<Button>().Height(36).BorderRadius(8),
    new Style<Label>().FontSize(14),
    new Style<Entry>().Height(40).BorderRadius(4),
]);
app.Run();
```

To update global styles at runtime (fires `GlobalStylesChanged`):

```csharp
UIApplication.Current?.UseGlobalStyles(newStyleSheet);
```

### 12.2 Component styles (UserControl)

Override `BuildStyles()` in any `UserControl`. Applied after global styles, so they take priority at equal specificity.

```csharp
public class MyCard : UserControl
{
    protected override StyleSheet? BuildStyles() =>
    [
        new Style<Button>().Height(32).BorderRadius(6),
        new Style<Button>(".cta")
            .Background(new Color(0, 120, 212))
            .When(StyleTrigger.Hover, s => s.Background(new Color(0, 100, 190))),
    ];

    public override VisualElement Build() => ...;
}
```

### 12.3 StyleEngine — manual application

```csharp
// Apply all rules in sheet to the subtree rooted at root
StyleEngine.Apply(sheet, root);

// With an explicit style scope
StyleEngine.Apply(sheet, root, StyleScope.Local);
```

### 12.4 GetComputedStyle — debugging

Returns the list of rules that would be applied to an element, including whether each one matched.

```csharp
var computed = StyleEngine.GetComputedStyle(myButton, sheet);

foreach (var match in computed)
{
    Console.WriteLine(
        $"[{(match.IsApplied ? "✓" : " ")}] " +
        $"Spec={match.Specificity} " +
        $"Important={match.IsImportant} " +
        $"{match.Rule.GetType().Name}");
}
```

---

## 13. Application priority

Rules are sorted and applied in the following order (last write wins on property conflict):

1. Normal rules ordered by **ascending specificity** (most specific applied last, wins).
2. At equal specificity: **declaration order** (later declaration wins).
3. **Global** sheet first, **component** sheet second (component wins over global at equal specificity).
4. `Important()` rules are applied at the very end, regardless of specificity or source.

```
Low priority ────────────────────────────── High priority

  Global,    type selector   (spec 1)
  Global,    class selector  (spec 11)
  Component, type selector   (spec 1)
  Component, class selector  (spec 11)
  Component, id selector     (spec 101)
  Component, id + class     (spec 111)
  Important()  ← always last
```

---

## 14. Reactivity — when styles are re-applied

The engine re-applies styles automatically in the following situations:

| Event                                   | Responsible        | Re-application scope                                     |
|-----------------------------------------|--------------------|----------------------------------------------------------|
| `IsHovered` / `IsPressed` changes       | `StyleApplier`     | Only rules with `When(StyleTrigger, ...)`                |
| `IsEnabled` changes                     | `StyleApplier`     | Only rules with `When(StyleTrigger.Disabled, ...)`       |
| Window crosses a named breakpoint       | `UserControl`      | Full pipeline (global + component)                       |
| Window resized (predicate conditions)   | `UserControl`      | Full pipeline (only if the sheet has predicate rules)    |
| OS color scheme changes                 | `UserControl`      | Full pipeline (only if the sheet has color-scheme rules) |
| Window orientation changes              | `UserControl`      | Full pipeline (only if the sheet has orientation rules)  |
| `UseTheme()` called                     | `UserControl`      | Full pipeline (all components)                           |
| `UseGlobalStyles()` called              | `UserControl`      | Full pipeline (all components)                           |
| Code hot reload                         | `HotReloadManager` | Full tree rebuild                                        |

Color-scheme polling occurs every ~300 frames (~5 seconds at 60 FPS).

---

## 15. Quick API reference

### Style\<T\> — Selectors

```csharp
new Style<T>()                          // type selector
new Style<T>("#id")                     // id selector
new Style<T>(".class")                  // class selector
new Style<T>(".a.b")                    // multi-class AND selector
new Style<T>("#id.class")               // id + class AND selector
Style.Id<T>("id")                       // factory: id selector
Style.Class<T>("class")                 // factory: class selector

.ChildOf<TParent>()                     // direct child of TParent  (>)
.DescendantOf<TAncestor>()              // descendant of TAncestor  (space)
.SiblingOf<TSibling>()                  // general sibling          (~)
.ImmediateSiblingOf<TSibling>()         // adjacent sibling         (+)
.FirstChild()                           // :first-child
.LastChild()                            // :last-child
.NthChild(step)                         // :nth-child(n), 1-based
.Not("#id")                             // :not — exclude by id
.Not(".class")                          // :not — exclude by class
.Not<TExclude>()                        // :not — exclude by type
```

### Style\<T\> — Conditions

```csharp
.When(StyleTrigger.Hover,    s => ...)  // :hover
.When(StyleTrigger.Pressed,  s => ...)  // :active
.When(StyleTrigger.Focused,  s => ...)  // :focus
.When(StyleTrigger.Disabled, s => ...)  // :disabled

.When(Breakpoint.XSmall, s => ...)     // < 480 px
.When(Breakpoint.Small,  s => ...)     // 480–767 px
.When(Breakpoint.Medium, s => ...)     // 768–1023 px
.When(Breakpoint.Large,  s => ...)     // 1024–1439 px
.When(Breakpoint.XLarge, s => ...)     // ≥ 1440 px

.When(w => w < 600, s => ...)          // custom width predicate (pixels)

.When(PlatformType.Windows, s => ...)
.When(PlatformType.MacOS,   s => ...)
.When(PlatformType.Linux,   s => ...)
.When(PlatformType.Android, s => ...)
.When(PlatformType.iOS,     s => ...)

.When(ColorScheme.Light, s => ...)
.When(ColorScheme.Dark,  s => ...)

.When(Rayo.Styling.Orientation.Portrait,  s => ...)
.When(Rayo.Styling.Orientation.Landscape, s => ...)

.When(ScreenDensity.Low,       s => ...)
.When(ScreenDensity.Normal,    s => ...)
.When(ScreenDensity.High,      s => ...)
.When(ScreenDensity.ExtraHigh, s => ...)
```

### Style\<T\> — Composition

```csharp
.Set(element => ...)                    // free setter (any property)
.Extend(otherStyle)                     // @extend — copy base setters
.WithTransition(durationMs)             // animate state changes (Out Quad default)
.WithTransition(durationMs, easing)     // animate with custom easing function
.Important()                            // !important
```

### Application and configuration

```csharp
// App-level
app.UseGlobalStyles(sheet)
app.UseTheme(theme)
UIApplication.Current?.ActiveTheme      // currently active theme
UIApplication.ThemeChanged              // static event
UIApplication.GlobalStylesChanged       // static event

// Component (override in UserControl)
protected override StyleSheet? BuildStyles() => [ ... ]
protected override StyleScope StyleScope => StyleScope.Local

// Manual
StyleEngine.Apply(sheet, root)
StyleEngine.Apply(sheet, root, StyleScope.Local)
StyleEngine.GetComputedStyle(element, sheet)  // returns IReadOnlyList<MatchedRule>

// Tokens
var tokens = new StyleTokens()
    .Set("--key", value)
    .Set("--key", t => t.Get<T>("--other"))   // computed (lazy)
tokens.Get<T>("--key")
tokens.Get<T>("--key", fallback)
tokens.TryGet<T>("--key", out value)
```

---

*Source: `Rayo/Styling/`*
