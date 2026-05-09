# Benchmark Results - Phase 4

Fecha de captura: `2026-05-09`

Condiciones:

- Runner: `Samples/PerformanceRunner`
- Backend: `SkiaSharp`
- Warmup: `30` frames
- Medicion: `120` frames
- `ContinuousRendering = true`
- `VSync = false`
- `TargetFPS = 240`

## Scroll

| Scenario | Avg FPS | Avg Frame Time | P95 Frame Time | Avg Render | Avg Elements Rendered | Avg Paint Dirty |
|---|---:|---:|---:|---:|---:|---:|
| `scroll (skia)` | 42.65 | 18.96 ms | 27.59 ms | 4.33 ms | 2.00 | 2.97 |

Notas:

- El escenario `scroll` usa un `ScrollView` grande con tarjetas y labels, moviendo `VerticalScrollOffset` continuamente.
- No hubo trabajo de `measure/arrange` durante la captura; la carga cae casi toda en repaint y rasterizacion.

## Editor

| Scenario | Avg FPS | Avg Frame Time | P95 Frame Time | Avg Render | Avg Elements Rendered | Avg Paint Dirty |
|---|---:|---:|---:|---:|---:|---:|
| `editor (skia)` antes de cache multiline | 41.29 | 19.88 ms | 28.95 ms | 8.79 ms | 5.00 | 3.97 |
| `editor (skia)` despues de cache multiline | 42.31 | 19.09 ms | 27.56 ms | 6.30 ms | 5.00 | 3.97 |
| `editor-wrap (skia)` despues de cache wrap | 42.43 | 19.10 ms | 27.58 ms | 5.03 ms | 5.00 | 3.97 |

Notas:

- El escenario `editor` usa un `Editor` con unas `900` lineas y scroll vertical continuo.
- La cache multiline de `TextBox` elimina `Split('\n')` y parte de los `Substring(...)` repetidos en render, hit testing y movimiento vertical.
- Tras ese cambio, `Avg Render` del escenario `editor` baja de `8.79 ms` a `6.30 ms`, una mejora aproximada del `28%`.
- El escenario `editor-wrap` mide un `Editor` con parrafos largos y `WordWrap(true)`. Con la cache de lineas envueltas y el reuse de `DisplayText`, su `Avg Render` queda en `5.03 ms`.

## Lectura provisional

- Las optimizaciones de la Fase 4 ya permiten medir escenarios dinamicos reales sin que aparezca trabajo extra de layout por frame.
- El cuello residual mas claro sigue estando en rendering de texto denso y desplazamiento de contenido textual.
- El siguiente paso con mas retorno ya no es solo cachear mas objetos, sino perfilar rutas concretas de `DrawTextWithFont(...)` y reducir mediciones por prefijo en seleccion/cursor para textos muy largos y hit testing horizontal.

## Comandos usados

```powershell
$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Rayo\Samples\PerformanceRunner\PerformanceRunner.csproj -- scroll 30 120

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Rayo\Samples\PerformanceRunner\PerformanceRunner.csproj -- editor 30 120

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Rayo\Samples\PerformanceRunner\PerformanceRunner.csproj -- editor 30 120

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Rayo\Samples\PerformanceRunner\PerformanceRunner.csproj -- editor-wrap 30 120
```
