# Rayo

Rayo is a declarative, retained-mode UI library for .NET 10.

It is designed for building desktop and cross-platform UI with a code-first model, a retained visual tree, a styling system, and multiple rendering backends.

## Highlights

- Declarative UI composition in C#
- Retained-mode rendering pipeline
- Signal-first reactive model
- Fluent API generated from `VisualElement` properties
- Styling system with selectors, triggers, and transitions
- Multiple rendering backends:
  - `Rayo.Rendering.OpenGL`
  - `Rayo.Rendering.SkiaSharp`
  - `Rayo.Rendering.Vulkan`
- Hosting packages for desktop and Android

## Package ecosystem

The repository currently publishes these NuGet packages together:

- `Rayo`
- `Rayo.FluentApiGenerator`
- `Rayo.Hosting.Abstractions`
- `Rayo.Hosting.Android`
- `Rayo.Hosting.Desktop`
- `Rayo.Rendering`
- `Rayo.Rendering.OpenGL`
- `Rayo.Rendering.SkiaSharp`
- `Rayo.Rendering.Vulkan`

All packages in the release set are versioned together from the same Git tag.

## Quick example

```csharp
using Rayo.Controls;
using Rayo.Layout;

var page =
    new VStack()
        .Spacing(12)
        .Padding(24)
        .Children(
            new Label()
                .Text("Hello from Rayo")
                .FontSize(24),
            new Button()
                .Text("Click Me")
                .Height(40)
        );
```

## Repository structure

- [Rayo](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo): core UI library
- [Rayo.Rendering](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo.Rendering): rendering contracts and primitives
- [Rayo.Rendering.OpenGL](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo.Rendering.OpenGL): OpenGL backend
- [Rayo.Rendering.SkiaSharp](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo.Rendering.SkiaSharp): SkiaSharp backend
- [Rayo.Rendering.Vulkan](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo.Rendering.Vulkan): Vulkan backend
- [Rayo.Hosting.Desktop](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo.Hosting.Desktop): desktop hosting
- [Rayo.Hosting.Android](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo.Hosting.Android): Android hosting
- [Samples](/C:/DEV/PROJECTS/RayoUI/Rayo/Samples): sample applications
- [Help](/C:/DEV/PROJECTS/RayoUI/Rayo/Help): technical documentation
- [Doc/NUGETS_GENERATION.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Doc/NUGETS_GENERATION.md): NuGet release guide

## Building locally

Restore and build the main library with the repository NuGet configuration:

```bash
dotnet restore Rayo/Rayo.csproj --configfile NuGet.Config
dotnet build Rayo/Rayo.csproj --no-restore
```

The solution file references missing projects in this environment, so direct project builds are the supported path.

## Documentation

- [Help/ARCHITECTURE.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/ARCHITECTURE.md)
- [Help/STYLE_ENGINE.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/STYLE_ENGINE.md)
- [Help/FLUENT_EXTENSIONS.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/FLUENT_EXTENSIONS.md)
- [Help/SOURCE_GENERATOR_NAMESPACE_MACROS.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/SOURCE_GENERATOR_NAMESPACE_MACROS.md)

## Samples

The repository includes sample applications such as:

- `Gallery`
- `FluentExamples`
- `StyleDemo`
- `OpenGLView`
- `VulkanView`
- `CrossPlatformApp`

## License

Rayo is distributed under the MIT License.

See [LICENSE](/C:/DEV/PROJECTS/RayoUI/Rayo/LICENSE) for the full text.
