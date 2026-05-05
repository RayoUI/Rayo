# AGENTS.md

Guidance for Codex when working in this repository.

## Workflow Rules

- **Never create git commits** unless the user explicitly asks.
- **All renderers must be updated together.** Any change to `IRenderer`, renderer-facing contracts, or the rendering pipeline must be applied to `Rayo.Rendering.OpenGL`, `Rayo.Rendering.SkiaSharp`, and `Rayo.Rendering.Vulkan`. If a fix is not applicable to one backend, add a comment explaining why.
- **Language is English - always.** All code comments, XML doc-comments, inline comments, doc files (`Help/`, `Doc/`), and user-facing strings must be in English.

## Build

```bash
dotnet build Rayo/Rayo.csproj
```

The solution file references missing projects — always build the `.csproj` directly.

## Architecture

Rayo is a declarative, retained-mode UI library on Silk.NET/OpenGL for .NET 10.

### Rendering backends and renderer contract

Rendering changes must be implemented in **all** backends:
- `Rayo.Rendering.OpenGL`
- `Rayo.Rendering.SkiaSharp`
- `Rayo.Rendering.Vulkan`

When adding or changing rendering features:
- update `Rayo.Rendering/IRenderer.cs` first,
- then keep behavior aligned across all three backends.

Render transforms are part of the contract:
- `IRenderer.PushTransform(Matrix3x2)`
- `IRenderer.PopTransform()`

### Layout pipeline
`Measure` -> `Arrange` -> `Render`. Call `MarkNeedsLayout()` when size/position may change, `MarkNeedsPaint()` for visual-only changes.

### Core classes

| Class | Purpose |
|---|---|
| `VisualElement` / `VisualElement<T>` | Base for all elements. Has `Id`, `Classes`, `IsHovered`, `IsPressed`, `IsEnabled`, `Parent`, computed bounds, `ZIndex` |
| `View<T>` | CRTP base for leaf controls with no children |
| `ContentView<T>` | Single-child container (`Frame`, `ScrollView`, etc.) |
| `CompositeView<T>` | Multi-child container |
| `UITree` | Owns root element; orchestrates measure/arrange/render |
| `UIApplication` | Window, render loop, global styles, DI, `EventManager` |
| `EventManager` | Hit-tests input, dispatches pointer/keyboard events, manages focus and `DragDropManager` |
| `HitTestEngine` | Depth-first hit-test respecting `ZIndex`. `IsInteractive()` = `IInputHandler` OR `IPointerHandler` — elements need one to be reachable by input |
| `DragDropManager` | Universal drag & drop; ghost rendering; `FindDraggableAt` blocks on interactive non-draggable overlays |
| `UserControl` | Reusable component base with `Build()`, lifecycle hooks, `BuildStyles()`, DI injection |
| `ViewBase<TViewModel>` / `ViewModelBase` | MVVM support |

### Input interfaces

**`IPointerHandler`** (modern, preferred) — bubbles up through parent chain:
- `OnPointerPressed`, `OnPointerReleased`, `OnPointerEntered`, `OnPointerExited`, `OnPointerMoved`
- All methods have default empty implementations.
- Element **must** implement this to be found by `HitTestEngine` in interactive mode.

**`IInputHandler`** (legacy, capture-based) — `HandleInput(InputEventArgs)` returns `bool` (consumed); also makes element interactive for hit-testing. Used for Slider, ScrollView, custom drag capture.

**`IDraggable`** / **`IDropTarget`** — drag & drop interfaces. `DragData` carries type string + payload + source element.

`IsEnabled` is inherited for interaction purposes: if an element or any ancestor is disabled, it must not receive focus, pointer events, keyboard input, or drag/input-handler dispatch. Input may fall through to an enabled ancestor instead.

### Styling

`Style<T>` targets elements by type + optional id/class selector. Applied by `StyleEngine` depth-first; specificity: type=1, each class=10, id=100.

```csharp
new Style<Button>()
    .Background(Colors.Blue)
    .When(StyleTrigger.Hover, s => s.Background(Colors.DarkBlue))
    .When(StyleTrigger.Pressed, s => s.Background(Colors.Navy))
    .WithTransition(150)

new Style<Button>(".primary")          // class selector
new Style<Button>("#submit")           // id selector
new Style<Button>().ChildOf<Frame>()   // direct child
```

`StyleScope.Local` on a `UserControl` stops global styles from cascading into it.
`StyleTokens`: `Set<T>(name, value)` / `Get<T>(name)`.

See `Help/STYLE_ENGINE.md` for current style-engine reference.

### Fluent DSL / source generator

All classes inheriting from `VisualElement` **automatically** get fluent extension methods generated for every public property. Method names are **identical to the property name** — no `Set` prefix.

```csharp
// Property: public string Text { get; set; }
// Generated: Button Text(string value)  - same name as property

new Button()
    .Text("Click Me")
    .Background(new Color(80, 120, 220))  // Color overload (implicit Color→Brush)
    .Height(36)
    .BorderRadius(8)
```

**CRTP pattern**: `public class MyWidget : View<MyWidget>` - fluent chain always returns the derived type.

**`[NoReactive]`** skips generation for a class or property:
- On a **class**: no fluent setters generated for any properties of that class
- On a **property**: only that specific property is skipped

Use cases for `[NoReactive]`:
- Properties with complex setters that shouldn't be exposed as fluent APIs
- Internal state properties that shouldn't be part of the public fluent interface
- Classes that inherit from `VisualElement` but should not have reactive generation

### Signals

Rayo uses a signals-first reactive model:
- `Signal<T>` for mutable state
- `Computed<T>` for derived state
- `Effect` for imperative reactions
- `SignalList<T>` for collection state

Prefer generated property overloads that accept `IReadableSignal<T>` over manual subscriptions when binding UI properties.
When a signal change will mutate the UI tree structure (`ClearChildren`, `AddChild`, `RemoveChild`, `Rebuild`), defer the work through `UIUpdateQueue.EnqueueUIUpdate(...)`.

### Common value types

- `Color` implicitly converts to `Brush`. Use `brush.PrimaryColor` to extract the `Color` back.
- `float` implicitly converts to `Thickness`.
- `CornerRadius`, `Size`, `Thickness` are value types.

### Picker dialogs

- `TimePicker`, `DatePicker`, and `ColorPicker` still work as trigger controls, but they also expose static `ShowDialog(...)` helpers for standalone modal launching.
- Use `ShowDialog(...)` when the user needs a custom launcher instead of rendering the built-in picker trigger in the layout.

### Creating a custom component

```csharp
// No attribute needed - automatic generation for all VisualElement descendants
public class MyWidget : View<MyWidget>
{
    public string Label
    {
        get => field;
        set => this.SetProperty(ref field, value, MarkNeedsPaint);
    }

    public override void Measure(float availableWidth, float availableHeight) { … }
    public override void Arrange(float x, float y, float width, float height) { … }
    public override void Render(IRenderer renderer) { … }
}

// Usage - fluent methods are auto-generated
new MyWidget()
    .Label("Hello")
    .Width(200)
    .Height(100);
```

To skip reactive generation for a specific property:

```csharp
public class MyWidget : View<MyWidget>
{
    public string Title { get; set; }  // ✓ Generates .Title() method

    [NoReactive]
    public string InternalState { get; set; }  // ✗ No .InternalState() method generated
}
```

### Dependency injection

```csharp
public class MyComponent : UserControl
{
    [Inject] public IMyService? MyService { get; private set; }

    public override VisualElement Build() { … }
}
```

Dependencies are injected through `DependencyInjector`, with `UIApplication` owning the application service provider.

## Keeping docs up to date

**AGENTS.md** and **`.github/copilot-instructions.md`** must be kept in sync with the code. Update both whenever you:
- Add a new system, base class, or architectural pattern
- Change an existing API (rename, retype, remove)
- Discover a gotcha or non-obvious behaviour

Outdated guidance produces incorrect code. Keep these files accurate.
