# Benchmark Results - Phase 1

Fecha de captura: `2026-05-08`

Condiciones:

- Runner: `Samples/PerformanceRunner`
- Warmup: `30` frames
- Medicion: `60` frames
- Modo: `idle` con `ContinuousRendering = true`
- `VSync = false`
- `TargetFPS = 240`

## Gallery

| Backend | Avg FPS | Avg Frame Time | P95 Frame Time | Avg Render | Elements Rendered |
|---|---:|---:|---:|---:|---:|
| OpenGL | 30.64 | 19.15 ms | 27.39 ms | 0.35 ms | 10 |
| SkiaSharp | 33.45 | 19.25 ms | 27.73 ms | 1.03 ms | 10 |

Notas:

- En `Gallery`, `OpenGL` redujo de forma clara el tiempo medio de render (`0.35 ms` vs `1.03 ms`).
- El `Avg FPS` no mejora de forma equivalente en esta captura, lo que sugiere que el cuello no es solo rasterizacion en este escenario `idle`.

## ToDoList

| Backend | Avg FPS | Avg Frame Time | P95 Frame Time | Avg Render | Elements Rendered |
|---|---:|---:|---:|---:|---:|
| OpenGL | 33.47 | 19.07 ms | 27.36 ms | 1.11 ms | 13 |
| SkiaSharp | 33.57 | 18.98 ms | 27.09 ms | 1.16 ms | 13 |

Notas:

- En `ToDoList`, ambos backends quedaron practicamente empatados en `idle`.
- La ventaja de `OpenGL` observada en `Gallery` no aparece de forma significativa aqui.

## Lectura provisional

- El cambio a `OpenGL` directo elimina el camino `SkiaSharp CPU -> upload completo a GL` del backend por defecto y eso ya era una mejora estructural necesaria.
- Con datos reales de `idle`, la mejora no es universal: depende de la app y del tipo de carga.
- El siguiente paso necesario para cerrar bien la Fase 1 es medir escenarios con movimiento real:
  - `scroll`
  - `resize`
  - `animations`

## Comandos usados

```powershell
$env:RAYO_DESKTOP_RENDERER='opengl'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 30 60

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- gallery 30 60

$env:RAYO_DESKTOP_RENDERER='opengl'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- todo 30 60

$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Samples\PerformanceRunner\PerformanceRunner.csproj -- todo 30 60
```
