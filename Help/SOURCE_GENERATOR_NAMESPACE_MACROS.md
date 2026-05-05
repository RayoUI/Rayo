# Source Generator — Namespace Macros

The `SourceGeneratorNamespace` MSBuild property controls where the source generator places the
generated `{ClassName}Extensions` static classes. It supports **macros** — curly-brace tokens
that are resolved per annotated class at generation time, letting you express dynamic namespace
patterns without hard-coding names.

---

## Table of Contents

1. [Quick start](#1-quick-start)
2. [Project wiring](#2-project-wiring)
3. [Macro reference](#3-macro-reference)
4. [Composition rules](#4-composition-rules)
5. [Examples](#5-examples)
6. [Default behaviour (no property set)](#6-default-behaviour-no-property-set)
7. [Consuming the generated namespace](#7-consuming-the-generated-namespace)

---

## 1. Quick start

```xml
<!-- TestExamples.csproj -->
<PropertyGroup>
  <SourceGeneratorNamespace>{root}.Extensions</SourceGeneratorNamespace>
</PropertyGroup>

<ItemGroup>
  <CompilerVisibleProperty Include="RootNamespace" />
  <CompilerVisibleProperty Include="SourceGeneratorNamespace" />
</ItemGroup>
```

For a class declared in `TestExamples.Controls`, the macro `{root}` expands to `TestExamples`,
so the generated extension class is placed in `TestExamples.Extensions`.

---

## 2. Project wiring

Two `<CompilerVisibleProperty>` declarations are required so the Roslyn generator can read the
MSBuild values at compile time:

```xml
<ItemGroup>
  <!-- Required for {assembly} macro — exposes the project's RootNamespace property -->
  <CompilerVisibleProperty Include="RootNamespace" />

  <!-- Required for the namespace template itself -->
  <CompilerVisibleProperty Include="SourceGeneratorNamespace" />
</ItemGroup>
```

If `RootNamespace` is not exposed, the `{assembly}` macro silently falls back to `{root}`.

> **Note:** If your project also sets `EmitCompilerGeneratedFiles=true` and
> `CompilerGeneratedFilesOutputPath=Generated`, add `<Compile Remove="Generated\**" />` to
> prevent the disk-cached generated files from being compiled a second time alongside the
> in-memory generator output.

---

## 3. Macro reference

All macros are **case-insensitive** and enclosed in curly braces. They are resolved
**per annotated class**, so different classes in the same project can produce different
namespaces from the same template.

| Macro | Resolves to | Fallback |
|---|---|---|
| `{class}` | Full namespace of the annotated class | Class name (when class has no namespace) |
| `{root}` | First dotted segment of the class namespace | Class name (when class has no namespace) |
| `{assembly}` | Project `RootNamespace` MSBuild property | Same as `{root}` |
| `{null}` | Global namespace — no `namespace { }` wrapper emitted | — |

### `{class}`

Expands to the **complete namespace** of the annotated class as declared in source.

| Class declaration | `{class}` |
|---|---|
| `namespace MyApp.Controls;` | `MyApp.Controls` |
| `namespace MyApp.Controls.Widgets;` | `MyApp.Controls.Widgets` |
| _(global namespace)_ | `MyClassName` (class name fallback) |

### `{root}`

Expands to the **first dotted segment** of the class namespace. Useful when you want all
extensions to live directly under the project's top-level namespace regardless of how deeply
the class is nested.

| Class declaration | `{root}` |
|---|---|
| `namespace MyApp.Controls;` | `MyApp` |
| `namespace MyApp.Controls.Widgets;` | `MyApp` |
| _(global namespace)_ | `MyClassName` (class name fallback) |

### `{assembly}`

Expands to the **`RootNamespace` MSBuild property** of the consuming project. This is
independent of the class namespace, making it stable even when classes live in varied
sub-namespaces. Falls back to `{root}` when `RootNamespace` is not exposed via
`CompilerVisibleProperty`.

```xml
<!-- MyLib.csproj -->
<RootNamespace>MyLib</RootNamespace>
<SourceGeneratorNamespace>{assembly}.Ext</SourceGeneratorNamespace>
<!-- All extensions → MyLib.Ext, regardless of which namespace each class lives in -->
```

### `{null}`

Signals that **no namespace wrapper** should be emitted. The generated extension class is
placed in the global namespace. Mainly useful for simple single-file projects or special
interop scenarios.

When `{null}` appears as the entire template value (after other macros are expanded) the
generator returns the global namespace. When `{null}` appears inside a larger expression it
is stripped and any resulting stray dots are collapsed automatically.

---

## 4. Composition rules

Macros can be freely combined with literal namespace segments using dot notation.

```
{root}.Extensions          →  MyApp.Extensions
{class}.Gen                →  MyApp.Controls.Gen
{assembly}.Generated       →  MyLib.Generated
My.Fixed.Ns.{root}         →  My.Fixed.Ns.MyApp
{root}.{assembly}.Shared   →  MyApp.MyLib.Shared  (unusual but valid)
{null}                     →  (global namespace)
```

**Dot cleanup:** if a macro expands to an empty string (e.g. `{null}` inside a longer
expression), consecutive dots are collapsed and leading/trailing dots are trimmed, so the
result is always a valid namespace identifier or `null` (global).

**Template absent or empty:** when `<SourceGeneratorNamespace>` is not set, or is empty /
whitespace, the generator uses each class's own namespace — identical to the behaviour before
the property existed.

---

## 5. Examples

### All extensions under a single fixed namespace

```xml
<SourceGeneratorNamespace>MyApp.Extensions</SourceGeneratorNamespace>
```

Every annotated class in the project, regardless of its own namespace, gets its extension
class placed in `MyApp.Extensions`.

### Sibling `.Extensions` namespace (most common pattern)

```xml
<SourceGeneratorNamespace>{root}.Extensions</SourceGeneratorNamespace>
```

| Class namespace | Generated namespace |
|---|---|
| `MyApp.Controls` | `MyApp.Extensions` |
| `MyApp.Layout` | `MyApp.Extensions` |
| `MyApp.Controls.Widgets` | `MyApp.Extensions` |

One `using MyApp.Extensions;` in each file gives access to all extension methods.

### Extensions nested under each class namespace

```xml
<SourceGeneratorNamespace>{class}.Gen</SourceGeneratorNamespace>
```

| Class namespace | Generated namespace |
|---|---|
| `MyApp.Controls` | `MyApp.Controls.Gen` |
| `MyApp.Layout` | `MyApp.Layout.Gen` |

### Project root namespace from MSBuild

```xml
<RootNamespace>Acme.Ui</RootNamespace>
<SourceGeneratorNamespace>{assembly}.Extensions</SourceGeneratorNamespace>
```

All generated classes → `Acme.Ui.Extensions`.

### Global namespace (no wrapper)

```xml
<SourceGeneratorNamespace>{null}</SourceGeneratorNamespace>
```

No `namespace { }` block is emitted. Extension methods are accessible without any `using`
directive (they are already in scope everywhere).

### Default — class's own namespace

```xml
<!-- Leave empty or omit entirely -->
<SourceGeneratorNamespace></SourceGeneratorNamespace>
```

Extension classes for `MyApp.Controls.Button` are placed in `MyApp.Controls` — they are
automatically in scope for any code that already uses that namespace.

---

## 6. Default behaviour (no property set)

When `SourceGeneratorNamespace` is absent, empty, or whitespace the generator falls back to
placing each extension class in the **same namespace as the annotated class**. This is the
historical default and requires no `using` directives beyond what the class itself needs.

---

## 7. Consuming the generated namespace

When a namespace override is active, callers must add a `using` directive for the resolved
namespace to access the extension methods.

```csharp
// When template is {root}.Extensions and root is TestExamples:
using TestExamples.Extensions;

// When template is {class}.Gen and class is in MyApp.Controls:
using MyApp.Controls.Gen;

// When template is {null} (global namespace): no using needed.
```

A single `using` at the top of each file (or in a global `GlobalUsings.cs`) is sufficient
even when the template produces the same namespace for all classes in the project.

```csharp
// GlobalUsings.cs
global using TestExamples.Extensions;
```
