# Rayo Architecture

## Overview

Rayo is a declarative, retained-mode UI library for .NET 10.

The library is organized around a retained visual tree, a three-phase layout pipeline, a signal-first reactivity model, a style engine, and multiple rendering and hosting packages.

## Project layout

The repository is split into a core library plus rendering, hosting, generator, and sample projects.

### Core library

- `Rayo`: visual tree, controls, layout, input, styling, reactivity hooks, assets, and application lifecycle

### Rendering contracts and backends

- `Rayo.Rendering`: shared rendering contracts and primitives
- `Rayo.Rendering.OpenGL`: OpenGL backend
- `Rayo.Rendering.SkiaSharp`: SkiaSharp backend
- `Rayo.Rendering.Vulkan`: Vulkan backend

### Hosting packages

- `Rayo.Hosting.Abstractions`: hosting contracts
- `Rayo.Hosting.Desktop`: desktop hosting
- `Rayo.Hosting.Android`: Android hosting

### Source generator

- `Rayo.FluentApiGenerator`: Roslyn component that generates fluent property extension methods

### Samples

- `Samples/*`: example applications and playgrounds

## Core runtime model

### Visual tree

The visual tree is built from `VisualElement` descendants.

Important base types:

- `VisualElement` / `VisualElement<T>`: common base for all visuals
- `View<T>`: leaf controls
- `ContentView<T>`: single-child container
- `CompositeView<T>`: multi-child container
- `UserControl`: reusable component with `Build()` and `BuildStyles()`

### Layout pipeline

Rayo uses a retained three-phase pipeline:

1. `Measure`
2. `Arrange`
3. `Render`

Use:

- `MarkNeedsLayout()` when size, position, or child structure may change
- `MarkNeedsPaint()` for visual-only invalidation

### Application lifecycle

`UIApplication` owns the application runtime, including:

- the root window
- render loop
- global styles
- active theme
- dependency injection
- event processing

`UITree` owns the current root element and coordinates layout and rendering.

## Rendering architecture

Rendering is abstracted through `Rayo.Rendering`.

Important contracts include:

- `IRenderer`
- `IGraphicsContext`
- `ITexture`
- `IFont`

Rendering behavior must stay aligned across:

- `Rayo.Rendering.OpenGL`
- `Rayo.Rendering.SkiaSharp`
- `Rayo.Rendering.Vulkan`

Render transforms are part of the renderer contract:

- `IRenderer.PushTransform(Matrix3x2)`
- `IRenderer.PopTransform()`

## Input architecture

Input is handled through hit testing and event dispatch in `EventManager`.

Primary interfaces:

- `IPointerHandler`: modern bubbling pointer model
- `IInputHandler`: legacy capture-oriented model
- `IDraggable` / `IDropTarget`: drag-and-drop contracts

`HitTestEngine` respects `ZIndex` and only treats elements as interactive when they implement `IPointerHandler` or `IInputHandler`.

Disabled state is inherited through ancestors. A disabled branch must not receive focus, pointer input, keyboard input, or drag/input-handler dispatch.

## Reactivity model

Rayo uses a signals-first model:

- `Signal<T>` for mutable state
- `Computed<T>` for derived state
- `Effect` for imperative reactions
- `SignalList<T>` for collection state

Inside `Build()`, prefer hooks-based ownership:

- `Hooks.UseSignal(...)`
- `Hooks.UseComputed(...)`
- `Hooks.UseEffect(...)`

If a signal change causes structural UI mutations such as `AddChild`, `RemoveChild`, `ClearChildren`, or rebuild work, defer it through `UIUpdateQueue.EnqueueUIUpdate(...)`.

## Styling model

The style engine is CSS-inspired and works on `Style<T>` rules applied to element subtrees.

Supported concepts include:

- type, id, and class selectors
- structural selectors such as `ChildOf`, `DescendantOf`, and `NthChild`
- conditional blocks with `When(...)`
- transitions
- design tokens
- themes
- style scoping through `StyleScope`

Global styles are provided through `UIApplication.UseGlobalStyles(...)`.
Component-local styles are declared by overriding `UserControl.BuildStyles()`.

## Fluent API generation

All classes inheriting from `VisualElement` automatically receive generated fluent property extension methods for their public properties unless excluded with `[NotFluent]`.

Generated methods keep the property name itself. For example:

```csharp
new Button()
    .Text("Save")
    .Height(36)
    .BorderRadius(8);
```

The generator can also place extension classes in a custom namespace through the `SourceGeneratorNamespace` MSBuild property. See [SOURCE_GENERATOR_NAMESPACE_MACROS.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/SOURCE_GENERATOR_NAMESPACE_MACROS.md).

## Platform support

The repository currently contains public rendering and hosting packages for:

- Desktop
- Android
- OpenGL
- SkiaSharp
- Vulkan

Platform-specific behavior is also represented in `PlatformOptions` and `PlatformType`.

## Current documentation map

For more detail, see:

- [STYLE_ENGINE.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/STYLE_ENGINE.md)
- [FLUENT_EXTENSIONS.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/FLUENT_EXTENSIONS.md)
- [COMPUTED_LIFECYCLE.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/COMPUTED_LIFECYCLE.md)
- [SIGNALS_BEST_PRACTICES.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/SIGNALS_BEST_PRACTICES.md)
- [SIGNAL_LIST_REACTIVITY.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/SIGNAL_LIST_REACTIVITY.md)
- [NUGETS_GENERATION.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/NUGETS_GENERATION.md)
