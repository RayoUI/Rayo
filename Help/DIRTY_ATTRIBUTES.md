# Dirty Attributes Guide

This guide explains how `Rayo` decides whether a property change should trigger a new `measure`, a new `arrange`, or only a repaint.

If you are creating your own controls, these attributes are the main way to plug into the layout invalidation pipeline without writing manual dirty logic in every setter.

## Overview

`Rayo` splits visual invalidation into three phases:

- `Measure`: recompute desired size.
- `Arrange`: recompute final placement using the current bounds.
- `Paint`: redraw without changing layout.

The source generator reads dirty attributes on public properties and wires them to the correct invalidation automatically.

In practice:

- `[MeasureProperty]` -> `InvalidateMeasure()`
- `[ArrangeProperty]` -> `InvalidateArrange()`
- `[PaintProperty]` -> `MarkNeedsPaint()`
- `[LayoutProperty]` -> compatibility alias for `InvalidateMeasure()`

When a property has no dirty attribute, the generator defaults to `MarkNeedsPaint()`.

## Which Attribute Should I Use?

Use this rule:

- If the property can change the control's desired size, use `[MeasureProperty]`.
- If the property only changes child placement or element position, use `[ArrangeProperty]`.
- If the property only changes visuals, use `[PaintProperty]`.

## MeasureProperty

`[MeasureProperty]` is for properties that affect how large the control wants to be.

Typical examples:

- text or content
- font size or font family
- padding or margin
- explicit width/height constraints
- item count that changes desired size
- visibility that removes the element from layout

Example:

```csharp
using Rayo.Controls;
using Rayo.Reactivity;

public class Badge : View<Badge>
{
    [MeasureProperty]
    public string Text
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = string.Empty;

    [MeasureProperty]
    public float PaddingX
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 8;

    protected override void MeasureUpdate(float availableWidth, float availableHeight)
    {
        float textWidth = Text.Length * 8;
        DesiredWidth = textWidth + PaddingX * 2;
        DesiredHeight = 24;
    }
}
```

Why `MeasureProperty` here:

- changing `Text` may change the desired width
- changing `PaddingX` definitely changes the desired width

## ArrangeProperty

`[ArrangeProperty]` is for properties that do not change desired size, but do change final placement.

Typical examples:

- scroll offsets
- local offsets used to place children
- alignment-like values that only affect final positioning
- splitter positions
- thumb positions

Example:

```csharp
using Rayo.Controls;
using Rayo.Reactivity;

public class OffsetHost : CompositeView<OffsetHost>
{
    [ArrangeProperty]
    public float ContentOffsetX
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    [ArrangeProperty]
    public float ContentOffsetY
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    protected override void MeasureUpdate(float availableWidth, float availableHeight)
    {
        foreach (var child in Children)
            child.Measure(availableWidth, availableHeight);

        DesiredWidth = availableWidth;
        DesiredHeight = availableHeight;
    }

    protected override void ArrangeUpdate(float x, float y, float width, float height)
    {
        base.ArrangeUpdate(x, y, width, height);

        foreach (var child in Children)
            child.Arrange(x + ContentOffsetX, y + ContentOffsetY, child.DesiredWidth, child.DesiredHeight);
    }
}
```

Why `ArrangeProperty` here:

- the content is still the same size
- only its final position changes

## PaintProperty

`[PaintProperty]` is for purely visual state.

Typical examples:

- fill color
- border color
- background
- opacity
- icon tint
- stroke brush

Example:

```csharp
using Rayo.Controls;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;

public class Dot : View<Dot>
{
    [PaintProperty]
    public Brush Fill
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Red;

    protected override void MeasureUpdate(float availableWidth, float availableHeight)
    {
        DesiredWidth = 10;
        DesiredHeight = 10;
    }
}
```

Why `PaintProperty` here:

- changing `Fill` does not affect size or placement
- only rendering changes

## LayoutProperty

`[LayoutProperty]` still works, but it is now a compatibility alias for measure invalidation.

Use it when you are touching older code or maintaining existing controls.

For new controls, prefer:

- `[MeasureProperty]`
- `[ArrangeProperty]`
- `[PaintProperty]`

## How The Generator Uses These Attributes

The source generator inspects each public property and emits the fluent setter plus dirty registration.

That means this:

```csharp
[MeasureProperty]
public int ColumnCount
{
    get => field;
    set => this.SetProperty(ref field, value);
}
```

behaves like:

- generated fluent method `ColumnCount(...)`
- automatic registration of `ColumnCount` as a measure-invalidating property
- automatic call to `InvalidateMeasure()` when the property changes

You do not need to manually call `InvalidateMeasure()` inside the normal setter when the attribute already describes the correct behavior.

## MeasureUpdate And ArrangeUpdate

Custom controls should put their layout logic in:

- `MeasureUpdate(...)`
- `ArrangeUpdate(...)`

The public wrappers:

- `Measure(...)`
- `Arrange(...)`

are owned by the framework and handle:

- dirty checks
- cached measure reuse
- arrange short-circuiting
- phase promotion (`measure` implies later `arrange` and `paint`)

Use the `Update` methods to define layout behavior, not to decide whether the phase should run.

## Manual Invalidation

Sometimes the property attribute is not enough, or the state is not represented by a generated public property.

Use manual invalidation when:

- you mutate internal state directly
- the change is caused by user interaction, not a property setter
- the dirty phase depends on runtime logic

Examples:

```csharp
InvalidateMeasure();
InvalidateArrange();
MarkNeedsPaint();
```

Good uses:

- internal text layout cache changed -> `InvalidateMeasure()`
- scroll offset changed programmatically -> `InvalidateArrange()`
- hover state changed color only -> `MarkNeedsPaint()`

## Decision Table

| Change | Attribute / API |
|---|---|
| Changes desired size | `[MeasureProperty]` or `InvalidateMeasure()` |
| Changes placement only | `[ArrangeProperty]` or `InvalidateArrange()` |
| Changes visuals only | `[PaintProperty]` or `MarkNeedsPaint()` |

## Common Mistakes

### 1. Using MeasureProperty for paint-only state

Bad:

```csharp
[MeasureProperty]
public Brush BorderBrush { get; set; }
```

This causes unnecessary layout work.

Use `[PaintProperty]` instead.

### 2. Using PaintProperty for size-changing text

Bad:

```csharp
[PaintProperty]
public string Text { get; set; }
```

If `Text` changes width or height, the control will render with stale layout.

Use `[MeasureProperty]`.

### 3. Calling Arrange(...) from custom code when you really need a forced local rearrange

The public `Arrange(...)` wrapper can skip execution when:

- the element is not dirty
- the rect is unchanged

If your control updates internal placement state without changing bounds, mark arrange dirty first or use the framework path that forces local arrange when appropriate.

### 4. Forgetting that no attribute means paint

If you omit the dirty attribute, generated properties default to repaint only.

That is safe for visuals, but wrong for properties that affect layout.

## Recommended Pattern For Custom Controls

```csharp
using Rayo.Controls;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;

public class StatusPill : View<StatusPill>
{
    [MeasureProperty]
    public string Text
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = string.Empty;

    [MeasureProperty]
    public float HorizontalPadding
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 10;

    [PaintProperty]
    public Brush Fill
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Blue;

    protected override void MeasureUpdate(float availableWidth, float availableHeight)
    {
        float textWidth = Text.Length * 8;
        DesiredWidth = textWidth + HorizontalPadding * 2;
        DesiredHeight = 28;
    }

    protected override void ArrangeUpdate(float x, float y, float width, float height)
    {
        base.ArrangeUpdate(x, y, width, height);
    }
}
```

This pattern gives you:

- correct layout invalidation
- correct fluent setter generation
- minimum work when only visuals change

## Related Files

- [ARCHITECTURE.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/ARCHITECTURE.md)
- [FLUENT_EXTENSIONS.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/FLUENT_EXTENSIONS.md)
- [SOURCE_GENERATOR_NAMESPACE_MACROS.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/SOURCE_GENERATOR_NAMESPACE_MACROS.md)
