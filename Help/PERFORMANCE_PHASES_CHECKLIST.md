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

- [x] Integrar `DirtyRegionTracker` en `UITree`
- [x] Hacer que `MarkNeedsPaint()` reporte bounds sucios
- [x] Hacer que `MarkNeedsLayout()` reporte bounds y ramas afectadas
- [ ] Evitar render recursivo completo cuando una rama no intersecta regiones sucias
- [x] Reducir limpieza global de dirty flags
- [~] Conectar `FrameScheduler` al pipeline real
- [~] Introducir una primera capa retenida para overlays/dialogs y root

## Fase 3 - Virtualizacion

- [x] Diseñar `VirtualizingScrollView` o panel virtualizado
- [x] Virtualizar `ListView`
- [x] Virtualizar `TreeView`
- [x] Virtualizar `DataGrid`
- [x] Introducir recycling de contenedores/items

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
- La primera entrega de la Fase 2 ya conecta invalidacion por elemento con `UITree`, `DirtyRegionTracker` y `FrameScheduler`.
- El render parcial todavia no esta activado porque `UIApplication.OnRender()` sigue limpiando toda la superficie con `Clear(...)` antes de dibujar el frame completo.
- `UITree` ya expone una politica explicita de render parcial experimental, pero `UIApplication` la mantiene desactivada al informar que el frame actual arranca con clear completo.
- `UITree` ya reutiliza texturas para el `root` y para cada overlay/dialog mediante `LayerCache`, invalida esas capas en relayout global, separa la invalidacion del `root` respecto a cambios internos de overlays y evita relayout completo del `root` cuando el trabajo pendiente pertenece solo a overlays, dejando una primera base retained-mode antes de activar render parcial real por regiones.
- La Fase 3 queda implementada con paneles virtualizados internos para `ListView`, `DataGrid` y `TreeView`, recycling de contenedores visibles y soporte basico para mantener la seleccion dentro del viewport; el siguiente paso recomendado es medir escenarios largos y ajustar detalles de viewport, horizontal scroll y estabilidad visual.
