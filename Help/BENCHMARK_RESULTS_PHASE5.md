# Benchmark Results - Phase 5

Fecha de captura: `2026-05-09`

Condiciones:

- Runner: `Samples/PerformanceRunner`
- Backend: `SkiaSharp`
- Warmup: `30` frames
- Medicion: `120` frames
- `ContinuousRendering = true`
- `VSync = false`
- `TargetFPS = 240`

## Styles

| Scenario | Avg FPS | Avg Frame Time | P95 Frame Time | Avg Render | Avg Elements Rendered | Avg Layout Dirty | Avg Paint Dirty |
|---|---:|---:|---:|---:|---:|---:|---:|
| `styles (skia)` | 42.22 | 19.20 ms | 27.63 ms | 4.60 ms | 5.00 | 99.39 | 99.39 |

Notas:

- El escenario `styles` usa un grid grande de tarjetas dentro de un `UserControl` con `BuildStyles()`.
- En cada frame se alterna un lote de `12` tarjetas, labels y botones, cambiando clases como `active`, `accent` y `muted`.
- Esto estresa sobre todo la parte incremental del style engine: `ClassesChanged`, `ApplyToElement(...)`, suscripciones de `StyleApplier` y la nueva clasificación `layout` vs `paint`.
- Durante la captura no hubo trabajo medio de `measure/arrange`, así que la carga principal quedó concentrada en reaplicación de estilos y repaint.

## Lectura provisional

- La Fase 5 ya tiene una ruta de benchmark dedicada y estable para cambios continuos de clase en un árbol grande.
- El escenario mantiene `Avg Render` en `4.60 ms`, más cerca del coste de `scroll` que del de `editor`, lo que sugiere que el estilo incremental ya no está empujando una recascada global por frame.
- El siguiente paso con más valor no es tanto seguir tocando el style engine a ciegas, sino comparar este benchmark frente a futuras regresiones o añadir una variante con cambio de tema/breakpoint para cubrir el resto de invalidaciones condicionales.

## Comando usado

```powershell
$env:RAYO_DESKTOP_RENDERER='skia'
dotnet run --project Rayo\Samples\PerformanceRunner\PerformanceRunner.csproj -- styles 30 120
```
