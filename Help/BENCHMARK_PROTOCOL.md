# Benchmark Protocol

Protocolo para medir cambios de rendimiento del core de `RayoUI` de forma repetible.

## Objetivo

Tener una forma consistente de comparar:

- el backend desktop `OpenGL` directo
- el backend `SkiaSharp` opt-in
- futuras optimizaciones de layout, render, virtualizacion y estilos

## Escenarios base

Usar al menos una app representativa por categoria:

- `Samples/Gallery` para UI general
- `Samples/ToDoList` para listas y controles comunes
- `Samples/VisualScripting` para escena densa
- `Samples/Notepad` o `TextBox`/`Editor` para input y texto

## Escenarios a medir

En cada app medir:

1. `Idle`
2. `Hover`
3. `Scroll`
4. `Resize`
5. `Typing`
6. `Animation`

## Preparacion

- Ejecutar en la misma maquina y misma resolucion
- Mantener el mismo build (`Debug` o `Release`) entre comparaciones
- Cerrar herramientas no necesarias
- Esperar unos segundos antes de capturar para evitar warmup inicial
- Activar `PerformanceTracker.IsEnabled = true` o DevTools
- Limpiar historia antes de cada escenario con `PerformanceTracker.ClearFrameHistory()`

Para comparar backends en desktop sin cambiar codigo:

- `RAYO_DESKTOP_RENDERER=opengl`
- `RAYO_DESKTOP_RENDERER=skia`

## Captura recomendada

Tomar entre `60` y `120` frames por escenario y guardar:

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

Usar:

```csharp
var summary = Rayo.DevTools.PerformanceTracker.GetSummary(120);
var text = Rayo.DevTools.PerformanceTracker.FormatSummary("Gallery Scroll", 120);
Console.WriteLine(text);
```

Tambien puedes usar el runner automatizado:

```powershell
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 60 120
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- todo 60 120
```

Con selector de backend:

```powershell
$env:RAYO_DESKTOP_RENDERER='opengl'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 60 120

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 60 120
```

## Plantilla de resultado

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

## Criterios de mejora

Para cambios de backend/render:

- bajar `Avg Frame Time`
- bajar `P95 Frame Time`
- bajar `Avg Render`

Para cambios de layout:

- bajar `Avg Measure`
- bajar `Avg Arrange`
- bajar `Avg Layout Dirty`

Para virtualizacion:

- bajar `Avg Elements Measured`
- bajar `Avg Elements Arranged`
- bajar `Avg Elements Rendered`

## Regla practica

No marcar una fase como cerrada solo por sensacion visual.
Siempre acompañar el cierre con al menos una tabla comparativa `antes/despues`.
