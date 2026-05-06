# Source Generator - Namespace Macros

The `SourceGeneratorNamespace` MSBuild property controls where the fluent API generator places generated `{ClassName}Extensions` static classes.

It supports namespace macros enclosed in `{...}` so a project can choose a fixed namespace, a namespace derived from each class, or the global namespace.

## Quick start

```xml
<PropertyGroup>
  <SourceGeneratorNamespace>{root}.Extensions</SourceGeneratorNamespace>
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleProperty Include="RootNamespace" />
  <CompilerVisibleProperty Include="SourceGeneratorNamespace" />
</ItemGroup>
```

For a class declared in `MyApp.Controls`, `{root}` expands to `MyApp`, so generated extension classes are emitted under `MyApp.Extensions`.

## Required project wiring

If you want the generator to read project-level namespace settings at compile time, expose them with `CompilerVisibleProperty`:

```xml
<ItemGroup>
  <CompilerVisibleProperty Include="RootNamespace" />
  <CompilerVisibleProperty Include="SourceGeneratorNamespace" />
</ItemGroup>
```

If your project also writes generated files to disk, exclude that output from compilation to avoid compiling generated code twice:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<ItemGroup>
  <Compile Remove="Generated\**" />
</ItemGroup>
```

## Macro reference

Macros are case-insensitive.

| Macro | Resolves to | Fallback |
|---|---|---|
| `{class}` | Full namespace of the annotated class | Class name when no namespace exists |
| `{root}` | First dotted segment of the class namespace | Class name when no namespace exists |
| `{assembly}` | Project `RootNamespace` | Same as `{root}` |
| `{null}` | Global namespace, with no `namespace` wrapper | None |

## Macro behavior

### `{class}`

Uses the complete namespace of the target class.

Examples:

- `MyApp.Controls` -> `MyApp.Controls`
- `MyApp.Controls.Widgets` -> `MyApp.Controls.Widgets`

### `{root}`

Uses the first dotted namespace segment.

Examples:

- `MyApp.Controls` -> `MyApp`
- `MyApp.Controls.Widgets` -> `MyApp`

### `{assembly}`

Uses the consuming project's `RootNamespace`.

Example:

```xml
<RootNamespace>Acme.Ui</RootNamespace>
<SourceGeneratorNamespace>{assembly}.Extensions</SourceGeneratorNamespace>
```

Generated namespace:

- `Acme.Ui.Extensions`

If `RootNamespace` is not exposed through `CompilerVisibleProperty`, this macro falls back to `{root}`.

### `{null}`

Places generated extension classes in the global namespace.

Example:

```xml
<SourceGeneratorNamespace>{null}</SourceGeneratorNamespace>
```

## Composition rules

Macros can be combined with literal namespace segments:

```text
{root}.Extensions
{class}.Generated
{assembly}.Fluent
My.Fixed.Namespace.{root}
{null}
```

Examples:

- `{root}.Extensions` -> `MyApp.Extensions`
- `{class}.Generated` -> `MyApp.Controls.Generated`
- `{assembly}.Fluent` -> `Acme.Ui.Fluent`

If macro expansion creates extra dots, the generator cleans them up automatically.

## Default behavior

If `SourceGeneratorNamespace` is missing, empty, or whitespace, generated extension classes are emitted in the same namespace as the source class.

That is the simplest setup and requires no extra `using` directives.

## Common patterns

### One shared extensions namespace per project

```xml
<SourceGeneratorNamespace>{root}.Extensions</SourceGeneratorNamespace>
```

Useful when you want a single `using` for all generated fluent methods.

### Namespace next to each class namespace

```xml
<SourceGeneratorNamespace>{class}.Generated</SourceGeneratorNamespace>
```

Useful when you want generated code grouped by feature area.

### Fixed project namespace

```xml
<SourceGeneratorNamespace>MyApp.Generated</SourceGeneratorNamespace>
```

Useful when you want a fully explicit and stable namespace layout.

## Consuming generated namespaces

If you override the generated namespace, callers must import the resolved namespace:

```csharp
using MyApp.Extensions;
```

If you want that import globally:

```csharp
global using MyApp.Extensions;
```

If you use `{null}`, no `using` directive is needed.
