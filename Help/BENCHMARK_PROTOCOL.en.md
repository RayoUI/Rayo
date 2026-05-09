# Benchmark Protocol

Protocol for measuring core performance changes in `RayoUI` in a repeatable way.

## Goal

Provide a consistent way to compare:

- the desktop OpenGL backend
- the opt-in SkiaSharp backend
- future optimizations to layout, rendering, virtualization and styles

## Base scenarios

Use at least one representative app per category:

- `Samples/Gallery` for general UI
- `Samples/ToDoList` for lists and common controls
- `Samples/VisualScripting` for dense-scene scenarios
- `Samples/Notepad` or `TextBox`/`Editor` for input and text

## Scenarios to measure

For each app measure:

1. `Idle`
2. `Hover`
3. `Scroll`
4. `Resize`
5. `Typing`
6. `Animation`

## Preparation

- Run on the same machine and resolution
- Keep the same build (`Debug` or `Release`) between comparisons
- Close unnecessary tools
- Wait a few seconds before capturing to avoid initial warmup
- Enable `PerformanceTracker.IsEnabled = true` or DevTools
- Clear history before each scenario with `PerformanceTracker.ClearFrameHistory()`

To compare desktop backends without changing code:

- `RAYO_DESKTOP_RENDERER=opengl`
- `RAYO_DESKTOP_RENDERER=skia`

## Recommended capture

Capture between `60` and `120` frames per scenario and save:

- `Avg FPS`
- `Avg Frame Time`
- `P95 Frame Time`
- `Avg Measure`
- `Avg Arrange`
- `Avg Render`
- `Avg Event`
- `Avg Elements Measured`
- `Avg Elements Arranged`
- `Avg Elements Rendered`
- `Avg Layout Dirty`
- `Avg Paint Dirty`

Use:

```csharp
var summary = Rayo.DevTools.PerformanceTracker.GetSummary(120);
var text = Rayo.DevTools.PerformanceTracker.FormatSummary("Gallery Scroll", 120);
Console.WriteLine(text);
```

You can also use the automated runner:

```powershell
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 60 120
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- todo 60 120
```

With backend selector:

```powershell
$env:RAYO_DESKTOP_RENDERER='opengl'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 60 120

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 60 120
```

## Result template

```text
Scenario: Gallery Scroll
Backend: OpenGL
Frames: 120
Avg FPS: ...
Avg Frame Time: ...
P95 Frame Time: ...
Avg Measure: ...
Avg Arrange: ...
Avg Render: ...
Avg Event: ...
Avg Elements Rendered: ...
```

## Improvement criteria

For backend/render changes:

- reduce `Avg Frame Time`
- reduce `P95 Frame Time`
- reduce `Avg Render`

For layout changes:

- reduce `Avg Measure`
- reduce `Avg Arrange`
- reduce `Avg Layout Dirty`

For virtualization:

- reduce `Avg Elements Measured`
- reduce `Avg Elements Arranged`
- reduce `Avg Elements Rendered`

## Rule of thumb

Do not mark a phase as closed based only on visual impression.
Always accompany closures with at least a before/after comparison table.
