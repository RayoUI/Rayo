# Rayo - AI Coding Assistant Guidelines

## Workflow Rules

- **Never create git commits** unless the user explicitly asks.
- **All renderers must be updated together.** Any change to `IRenderer`, renderer-facing contracts, or the rendering pipeline must be applied to `Rayo.Rendering.OpenGL`, `Rayo.Rendering.SkiaSharp`, and `Rayo.Rendering.Vulkan`. If a fix is not applicable to one backend, leave a short code comment explaining why.
- **Language is English - always.** All code comments, XML doc-comments, inline comments, doc files (`Help/`, `Doc/`), and user-facing strings must be in English.

## Build

```bash
dotnet build Rayo/Rayo.csproj
```

The solution file references missing projects, so build the `.csproj` directly.

## NuGet release automation

- NuGet packaging is opt-in. Only projects with `<IsPackable>true</IsPackable>` are intended for publication.
- The current publish set is tracked in `eng/nuget-pack-projects.txt` and released together from the same version tag.
- GitHub Actions uses `.github/workflows/ci.yml` for package validation and `.github/workflows/publish-nuget.yml` for tag-based publishing.
- NuGet publishing is designed for nuget.org Trusted Publishing with GitHub Actions OIDC plus the `NUGET_USER` repository secret.
- The current release set includes the hosting packages, so workflow changes must continue to support `Rayo.Hosting.Android` and `Rayo.Hosting.Desktop`.

## Architecture

Rayo is a declarative, retained-mode UI library on Silk.NET/OpenGL for .NET 10.

### Rendering backends and renderer contract

Rendering changes must stay aligned across:
- `Rayo.Rendering.OpenGL`
- `Rayo.Rendering.SkiaSharp`
- `Rayo.Rendering.Vulkan`

When adding or changing rendering features:
- update `Rayo.Rendering/IRenderer.cs` first,
- then implement the behavior in all three backends.

Render transforms are part of the contract:
- `IRenderer.PushTransform(Matrix3x2)`
- `IRenderer.PopTransform()`

Pointer and hit-test logic is transform-aware:
- use `VisualElement.GetLocalPosition(...)` for pointer-local coordinates,
- do not reintroduce raw `ComputedX` / `ComputedY` subtraction in input code.

### Layout pipeline

`Measure` -> `Arrange` -> `Render`.

- Call `MarkNeedsLayout()` when size, position, or child structure may change.
- Call `MarkNeedsPaint()` for visual-only changes.

### Core classes

| Class | Purpose |
|---|---|
| `VisualElement` / `VisualElement<T>` | Base for all elements. Has `Id`, `Classes`, `IsHovered`, `IsPressed`, `IsEnabled`, `Parent`, computed bounds, `ZIndex` |
| `View<T>` | CRTP base for leaf controls with no children |
| `ContentView<T>` | Single-child container (`Frame`, `ScrollView`, `UserControl`, etc.) |
| `CompositeView<T>` | Multi-child container |
| `UITree` | Owns the root element and orchestrates measure/arrange/render |
| `UIApplication` | Window, render loop, theme, global styles, DI, `EventManager` |
| `EventManager` | Hit-tests input, dispatches pointer/keyboard events, manages focus and `DragDropManager` |
| `HitTestEngine` | Depth-first hit-test respecting `ZIndex` |
| `DragDropManager` | Universal drag & drop, ghost rendering, threshold handling |
| `UserControl` | Reusable component base with `Build()`, lifecycle hooks, `BuildStyles()`, style scope, DI injection |
| `ViewBase<TViewModel>` / `ViewModelBase` | MVVM support |

### Layout containers

The built-in layout containers in `Rayo/Layout` are:
- `Absolute`
- `Flex`
- `Grid`
- `HStack`
- `LStack`
- `VStack`

### Input interfaces

**`IPointerHandler`** is the modern, preferred input surface. Pointer events bubble through the parent chain:
- `OnPointerPressed`
- `OnPointerReleased`
- `OnPointerEntered`
- `OnPointerExited`
- `OnPointerMoved`

All `IPointerHandler` methods have default empty implementations.

**`IInputHandler`** is the legacy capture-based interface:
- `HandleInput(InputEventArgs)` returns `bool` to indicate whether the event was consumed.
- It is still used for controls such as sliders, scroll capture, and custom drag behaviors.

Hit testing treats an element as interactive when it implements either `IPointerHandler` or `IInputHandler`.

`IsEnabled` is inherited for interaction purposes: if an element or any ancestor is disabled, it must not receive focus, pointer events, keyboard input, or drag/input-handler dispatch. Input may fall through to an enabled ancestor instead.

### Styling

`Style<T>` supports type, id, and class selectors with CSS-like specificity:
- type = `1`
- each class = `10`
- id = `100`

Examples:

```csharp
new Style<Button>()
    .Background(Colors.Blue)
    .When(StyleTrigger.Hover, s => s.Background(Colors.DarkBlue))
    .When(StyleTrigger.Pressed, s => s.Background(Colors.Navy))
    .WithTransition(150);

new Style<Button>(".primary");
new Style<Button>("#submit");
new Style<Button>("#submit.primary");
new Style<Button>().ChildOf<Frame>();
new Style<Button>().DescendantOf<Card>();
```

`Style<T>` also supports:
- breakpoints,
- custom width predicates,
- platform conditions,
- color-scheme conditions,
- orientation conditions,
- screen-density conditions,
- structural selectors such as `SiblingOf`, `ImmediateSiblingOf`, `FirstChild`, `LastChild`, `NthChild`, and `Not(...)`.

Style scoping rules:
- `UIApplication.UseGlobalStyles(...)` applies app-wide styles.
- `UserControl.BuildStyles()` provides component-scoped styles.
- `StyleScope.Local` prevents styles from cascading into nested `UserControl` boundaries.

Reference docs that currently exist in the repo:
- `Help/STYLE_ENGINE.md`
- `Help/ARCHITECTURE.md`

### Fluent DSL and source generator

All classes inheriting from `VisualElement` automatically get fluent extension methods generated for every public property. Generated method names match the property names exactly.

```csharp
public class MyWidget : View<MyWidget>
{
    public string Label
    {
        get => field;
        set => this.SetProperty(ref field, value, MarkNeedsPaint);
    }
}

new MyWidget()
    .Label("Hello")
    .Height(40);
```

`[NotFluent]` disables fluent generation:
- on a class: no generated fluent setters for that class,
- on a property: skip only that property.

### Signals

Rayo uses a signals-first reactive model:
- `Signal<T>` for mutable state,
- `Computed<T>` for derived state,
- `Effect` for imperative reactions,
- `SignalList<T>` for collection state.

Prefer generated property overloads that accept `IReadableSignal<T>` over manual subscriptions.

When a signal change mutates UI tree structure (`ClearChildren`, `AddChild`, `RemoveChild`, `Rebuild`), defer the change through `UIUpdateQueue.EnqueueUIUpdate(...)`.

Reference docs that currently exist in the repo:
- `Doc/SIGNALS_BEST_PRACTICES.md`
- `Doc/SIGNAL_LIST_REACTIVITY.md`
- `Doc/COMPUTED_LIFECYCLE.md`

### Dependency injection

Properties marked with `[Inject]` are populated through `DependencyInjector`.

Typical usage:

```csharp
public class MyComponent : UserControl
{
    [Inject] public IMyService? MyService { get; private set; }

    public override VisualElement Build() { ... }
}
```

`UIApplication` owns the application service provider, and `UserControl`, `ViewBase<TViewModel>`, and `ViewModelBase` participate in DI-aware flows.

### Picker dialogs

- `TimePicker`, `DatePicker`, and `ColorPicker` can be used either as trigger controls or as standalone modal pickers through their static `ShowDialog(...)` helpers.
- Prefer `ShowDialog(...)` when a screen needs a custom launch surface instead of the picker's built-in trigger UI.

## Keeping docs up to date

**`AGENTS.md`** and **`.github/copilot-instructions.md`** must stay in sync with the code. Update both when you:
- add a new system, base class, or architectural pattern,
- change an API,
- discover a gotcha or non-obvious behavior.

Outdated guidance produces incorrect code. Keep both files accurate.
