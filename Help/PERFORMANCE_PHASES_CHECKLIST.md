# RayoUI Performance Phases Checklist

Estado del plan de rendimiento del core de `RayoUI`.

## Leyenda

- [ ] Pendiente
- [~] En progreso
- [x] Completado

## Fase 1 - Backend desktop GPU-first

- [x] Definir el objetivo y documentar la estrategia de la fase
- [x] Cambiar el backend desktop por defecto de `SkiaSharp CPU + GL blit` a `OpenGL` directo
- [x] Mantener `SkiaSharp` como backend alternativo opt-in
- [x] Adaptar la inicializacion del `OpenGLGraphicsContext` al ciclo de vida de `UIApplication`
- [x] Dejar una base de benchmark y resumenes agregados para comparar backends
- [x] Añadir un selector simple de backend desktop para benchmarks (`RAYO_DESKTOP_RENDERER`)
- [~] Medir mejora frente al path anterior
- [ ] Completar mediciones de `scroll`, `resize` y `animations`
- [ ] Evaluar si `SkiaSharp` debe migrar a surface GPU-backed en vez de CPU-backed
- [ ] Revisar fallback o selector de backend por plataforma/GPU

## Fase 2 - Invalidacion parcial real

- [ ] Integrar `DirtyRegionTracker` en `UITree`
- [ ] Hacer que `MarkNeedsPaint()` reporte bounds sucios
- [ ] Hacer que `MarkNeedsLayout()` reporte bounds y ramas afectadas
- [ ] Evitar render recursivo completo cuando una rama no intersecta regiones sucias
- [ ] Reducir limpieza global de dirty flags
- [ ] Conectar `FrameScheduler` al pipeline real

## Fase 3 - Virtualizacion

- [ ] Diseñar `VirtualizingScrollView` o panel virtualizado
- [ ] Virtualizar `ListView`
- [ ] Virtualizar `TreeView`
- [ ] Virtualizar `DataGrid`
- [ ] Introducir recycling de contenedores/items

## Fase 4 - Reducir allocations y coste CPU del renderer

- [ ] Cache/pool de `SKPaint`
- [ ] Cache de geometria/path frecuentes
- [ ] Cache de medida y layout de texto
- [ ] Reducir creacion temporal de `SKFont`
- [ ] Eliminar LINQ y `ToArray()` en hot paths criticos

## Fase 5 - Style engine incremental

- [ ] Cachear indice compilado de reglas
- [ ] Evitar reconstruccion completa en cada `Apply()`
- [ ] Hacer `StyleApplier.Attach` idempotente
- [ ] Reaplicar solo reglas afectadas por cambio de estado/clase/tema
- [ ] Revisar clasificacion layout vs paint de propiedades

## Notas de implementacion

- `SkiaSharp` vuelve a ser el backend desktop por defecto por estabilidad.
- `OpenGL` queda disponible como backend experimental opt-in mediante `RAYO_DESKTOP_RENDERER=opengl`.
- La primera entrega de la Fase 1 se centra en quitar del camino por defecto el upload completo de la surface CPU de `SkiaSharp` a una textura OpenGL por frame.
- El cambio debe ser compatible con usuarios que sigan llamando manualmente a `SetGraphicsContext(new SkiaSharpGraphicsContext())`.
