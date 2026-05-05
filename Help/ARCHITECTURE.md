# Rayo - Arquitectura Agnóstica de Backend Gráfico

## Visión General

Rayo ha sido refactorizado para ser completamente independiente del backend gráfico, permitiendo intercambiar entre OpenGL, Vulkan, DirectX, Metal, u otros backends sin cambiar el código UI.

## Estructura de Proyectos

```
Rayo/                          # Core UI (agnóstico de gráficos)
├── Components/                    # Componentes UI (Button, TextBox, etc.)
├── Layout/                        # Sistemas de layout (VStack, HStack, Grid)
└── Core/                          # Lógica core (UITree, UIApplication, etc.)

Rayo.Rendering/                # Abstracciones de renderizado
├── IGraphicsContext.cs           # Contexto gráfico abstracto
├── IRenderer.cs                  # Interface de renderizado
├── ITexture.cs                   # Abstracción de textura
├── IShaderProgram.cs             # Abstracción de shader
└── IBuffer.cs                    # Abstracción de buffer

Rayo.Rendering.OpenGL/        # Implementación OpenGL
├── OpenGLGraphicsContext.cs
├── OpenGLRenderer.cs
├── OpenGLTexture.cs
├── OpenGLShaderProgram.cs
└── OpenGLBuffer.cs

Rayo.Rendering.Vulkan/        # Implementación Vulkan
├── VulkanGraphicsContext.cs
├── VulkanRenderer.cs
└── ...

Rayo.Example/                  # Aplicación de ejemplo
```

## Conceptos Clave

### 1. Abstracción de Contexto Gráfico

`IGraphicsContext` es la interface principal que abstrae el contexto gráfico:

```csharp
public interface IGraphicsContext : IDisposable
{
    IRenderer CreateRenderer();
    ITexture CreateTexture(int width, int height, byte[] data, TextureFormat format);
    IShaderProgram CreateShaderProgram(string vertexShader, string fragmentShader);
    // ... más métodos
}
```

### 2. Interface de Renderizado

`IRenderer` proporciona métodos de alto nivel para renderizar UI:

```csharp
public interface IRenderer : IDisposable
{
    void Initialize(int width, int height);
    void BeginFrame();
    void EndFrame();

    // Primitivas
    void DrawRect(float x, float y, float width, float height, Color color);
    void DrawCircle(float cx, float cy, float radius, Color color);

    // Texto
    void DrawText(string text, float x, float y, Color color, float fontSize);

    // Texturas
    void DrawTexture(ITexture texture, float x, float y, float width, float height);
}
```

### 3. UIApplication Agnóstico

`UIApplication` ya no depende directamente de OpenGL:

```csharp
public class UIApplication
{
    private IGraphicsContext? _graphicsContext;
    private IRenderer? _renderer;

    public void Initialize(IGraphicsContext context)
    {
        _graphicsContext = context;
        _renderer = context.CreateRenderer();
        _renderer.Initialize(Width, Height);
    }
}
```

## Uso - Selección de Backend

### OpenGL

```csharp
using Silk.NET.OpenGL;
using Rayo.Core;
using Rayo.Rendering.OpenGL;

var gl = window.CreateOpenGL();
var graphicsContext = new OpenGLGraphicsContext(gl);
var app = new UIApplication("My App", 800, 600);
app.Initialize(graphicsContext);
```

### Vulkan

```csharp
using Silk.NET.Vulkan;
using Rayo.Core;
using Rayo.Rendering.Vulkan;

var vk = Vk.GetApi();
var graphicsContext = new VulkanGraphicsContext(vk, instance, device);
var app = new UIApplication("My App", 800, 600);
app.Initialize(graphicsContext);
```

## Beneficios

1. **Portabilidad**: Cambiar de backend gráfico es trivial
2. **Testing**: Puedes crear un backend "Mock" para tests
3. **Performance**: Elegir el backend más apropiado para cada plataforma
4. **Futuro-proof**: Fácil agregar nuevos backends (WebGPU, Metal, etc.)

## Migración del Código Existente

El código UI existente (componentes, layouts) NO necesita cambios. Solo los siguientes archivos necesitan refactorización:

1. **UIRenderer.cs** → **OpenGLRenderer.cs** (mover a Rayo.Rendering.OpenGL)
2. **FontAtlas.cs** → Abstraer con **IFont** interface
3. **TextureManager.cs** → Usar **ITexture** interface
4. **UIApplication.cs** → Usar **IGraphicsContext** en lugar de GL directo

## Estado Actual

✅ Estructura de proyectos creada
✅ Interfaces de abstracción definidas
✅ OpenGLGraphicsContext implementado
✅ OpenGLTexture implementado
🔄 OpenGLRenderer (en progreso - usar código existente como base)
⏳ VulkanGraphicsContext (pendiente)
⏳ Refactorización de UIApplication (pendiente)

## Próximos Pasos

1. Completar OpenGLRenderer usando el código existente de UIRenderer como base
2. Refactorizar UIApplication para aceptar IGraphicsContext
3. Implementar backend Vulkan básico
4. Actualizar ejemplo para demostrar cambio de backend
5. Documentar proceso de migración completo
