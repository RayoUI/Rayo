# Avalonia Optimization Checklist

This document tracks the remaining layout and rendering optimizations that bring `Rayo` closer to the behavior and efficiency of `Avalonia`.

It is organized by execution phase, with emphasis on return on investment and practical implementation order.

## Status Legend

- `[ ]` pending
- `[~]` in progress
- `[x]` completed

## Phase 1: Fine-Grained Invalidation

Goal:

- replace broad `MarkNeedsLayout()` usage in hot paths
- prefer `InvalidateArrange()` when only placement changes
- prefer `MarkNeedsPaint()` when only visuals change

### Core rules

- `[x]` Introduce explicit `measure`, `arrange`, and `paint` invalidation phases.
- `[x]` Route generated property invalidation through dirty attributes.
- `[ ]` Remove legacy broad invalidation from high-frequency interaction paths.
- `[ ]` Audit manual invalidation in built-in controls.

### Priority files

- `[~]` [ScrollView.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/ScrollView.cs)
  - `[x]` force local arrange when scroll offsets change and bounds stay the same
  - `[x]` downgrade scroll-offset fallback invalidation to arrange-only
  - `[ ]` review remaining fallback `MarkNeedsLayout()` paths
- `[~]` [TabControl.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/TabControl.cs)
  - `[x]` remove redundant relayout after header scroll offset changes
  - `[~]` audit `UpdateScrollButtonStates()` for remaining broad invalidation
  - `[x]` convert structural tab rebuild/update paths to explicit measure invalidation
  - `[ ]` review tab add/remove/select paths for any further arrange-only opportunities
- `[~]` [Splitter.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/Splitter.cs)
  - `[x]` remove redundant broad invalidation after child size setters during drag-resize
  - `[ ]` classify any remaining drag-resize invalidation path
- `[ ]` [Drawer.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/Drawer.cs)
  - `[x]` separate animation-time arrange from full measure invalidation
  - `[x]` make structural drawer changes explicit measure invalidation
  - `[ ]` review overlay invalidation further
- `[~]` [Accordion.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/Accordion.cs)
  - `[x]` convert expand/collapse animation invalidation to explicit measure invalidation
  - `[ ]` review whether any animation-time path can become arrange-only
- `[~]` [TreeView.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/TreeView.cs)
  - `[x]` convert rebuild and virtualization configuration paths to explicit measure invalidation
  - `[x]` narrow virtualized header-only expand/collapse refresh to paint-only
  - `[ ]` review selection/expand/collapse paths further
- `[~]` [ListView.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/ListView.cs)
  - `[x]` convert virtualization refresh invalidation to explicit measure invalidation
  - `[ ]` review selection and item mutation paths further
- `[~]` [DataGrid.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/DataGrid.cs)
  - `[x]` convert rebuild and virtualization refresh invalidation to explicit measure invalidation
  - `[ ]` review header/data rebuild invalidation for narrower opportunities
- `[ ]` [Loading.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/Loading.cs)
  - `[ ]` confirm content swap paths really need measure invalidation
- `[ ]` [Editor.cs](/C:/DEV/PROJECTS/RayoUI/Rayo/Rayo/Controls/Editor.cs)
  - `[x]` switch wrapped-line cache invalidation to explicit measure invalidation
  - `[ ]` review scrollbar-only updates

### Phase 1 checklist by scenario

- `[ ]` scrolling repositions content without forcing full re-measure
- `[ ]` drag interactions use arrange invalidation where possible
- `[ ]` hover/pressed/selection visual updates stay paint-only where size is unchanged
- `[ ]` expand/collapse animations use the narrowest valid phase
- `[ ]` virtualization refreshes do not escalate to root measure unless extent changes

### Notes

- `MarkNeedsLayout()` is still a compatibility shim to `InvalidateMeasure()`.
- Treat every remaining `MarkNeedsLayout()` in a hot path as suspicious until reviewed.
- Prefer correctness over narrow invalidation when the affected size contract is unclear.

## Phase 2: Layout Telemetry

Goal:

- measure how much work is executed, skipped, or escalated

### Checklist

- `[x]` count `measure executed`
- `[x]` count `measure skipped`
- `[x]` count `measure cache hits/misses`
- `[x]` count `arrange executed`
- `[x]` count `arrange skipped`
- `[x]` count relayout roots per frame
- `[x]` expose stats in devtools response payload
- `[x]` record coarse invalidation causes

## Phase 3: Better Relayout Root Selection

Goal:

- re-layout the smallest safe subtree

### Checklist

- `[ ]` refine measure relayout-root heuristics
- `[~]` refine arrange relayout-root heuristics
- `[~]` recognize explicit-size boundaries
- `[~]` recognize scroll/viewport hosts that absorb descendant measure changes under finite constraints
- `[~]` recognize overlay roots independently
- `[~]` deduplicate overlapping dirty branches more aggressively

## Phase 4: Virtualization And Recycling

Goal:

- reduce churn in large scrollable collections

### Checklist

- `[~]` viewport-based realization in `ListView`
- `[~]` viewport-based realization in `TreeView`
- `[~]` viewport-based realization in `DataGrid`
- `[~]` recycling pools for realized containers
- `[~]` reduce create/dispose churn during scroll

## Phase 5: Richer Measure Cache

Goal:

- reuse more layout work safely

### Checklist

- `[~]` extend cache beyond the last constraint pair
- `[ ]` invalidate cache more selectively
- `[~]` add cache hooks for expensive text/layout controls

## Recommended Execution Order

1. Phase 1
2. Phase 2
3. Phase 3
4. Phase 4
5. Phase 5

## Current Progress

- `[x]` Measure / arrange / paint pipeline split implemented
- `[x]` property dirty attributes documented in [DIRTY_ATTRIBUTES.md](/C:/DEV/PROJECTS/RayoUI/Rayo/Help/DIRTY_ATTRIBUTES.md)
- `[~]` Phase 1 started with scroll, tab-header, splitter, drawer, accordion, tree, list, datagrid, and editor invalidation cleanup
- `[~]` Phase 1 also now narrows several remaining broad invalidations in `Loading`, `Checkbox`, `Splitter`, `Card`, `SideBar`, and animation paths that were still escalating to generic layout work
- `[~]` Core composition/container APIs (`CompositeView`, `ContentView`, `Layout`, `UserControl`, `Flex`, `Grid`) and DevTools property editing now also use explicit measure invalidation instead of relying on the legacy `MarkNeedsLayout()` shim
- `[~]` Phase 3 started by batching incremental arrange and measure work by effective arrange host, avoiding repeated parent `Arrange` passes for sibling dirty branches
- `[~]` Measure invalidation and relayout-root selection now stop at explicit-size parents that absorb descendant size changes
- `[~]` `ScrollView` now acts as a semantic measure boundary when it was measured with finite viewport constraints, reducing relayout escalation from virtualized content
- `[~]` `ScrollView` now also absorbs descendant arrange changes once arranged, so arrange-root selection stops at the scroll host instead of climbing into unrelated ancestors
- `[~]` Incremental overlay layout now distinguishes `measure+arrange` from `arrange-only`, instead of always remeasuring overlay roots
- `[x]` Performance telemetry now distinguishes `measure dirty`, `arrange dirty`, and `paint dirty`, and exposes the phase in the dirty log payload
- `[~]` Virtualized `ListView`, `TreeView`, and `DataGrid` now avoid rebinding unchanged visible containers on simple viewport shifts, reducing scroll churn on reused elements
- `[~]` Virtualized `ListView`, `TreeView`, and `DataGrid` now also avoid rebuilding `Children` when the visible range is unchanged and only bindings refresh, reducing tree-structure churn
- `[~]` Virtualized `ListView`, `TreeView`, and `DataGrid` now update partially overlapping visible ranges in place, instead of rebuilding the whole materialized collection on every small scroll shift
- `[x]` Virtualization telemetry now reports created, reused, rebound, and recycled containers per frame for virtualized `ListView`, `TreeView`, and `DataGrid`
- `[~]` Virtualized panel `Configure(...)` paths now downgrade to `InvalidateArrange()` when the total viewport extent is unchanged, avoiding unnecessary re-measure on data refreshes with stable counts/heights
- `[~]` `Label` now caches intrinsic multiline text measurement inputs and reuses them across repeated measure/render passes when text/font/line-height/padding are unchanged
- `[~]` `TextBox`/`Editor` now cache repeated `MeasureTextWidth(...)` fragment measurements behind renderer/font/password-sensitive invalidation, reducing repeated substring measurement during wrapping, cursor math, and hit-testing
- `[~]` `Editor` now validates wrapped-line and max-line-width caches against renderer, font size, and password mode, preventing stale fallback-based text layout from surviving into render-time measurement
- `[~]` `Editor` now precomputes wrapped-line prefix widths and reuses them for selection, cursor placement, and mouse hit-testing, avoiding repeated substring measurement during interactive wrapped-text editing
- `[~]` `TextBox` now precomputes single-line and multiline prefix widths for cursor visibility, selection, and mouse hit-testing, and `Editor` reuses that cache for the non-wrapped scrolling path
- `[~]` `VisualElement` now keeps a small per-element multi-constraint measure cache instead of only remembering the most recent constraint pair, reducing remeasure churn when a subtree toggles between a few stable sizes
- `[~]` `VisualElement` now also normalizes measure-cache keys across equivalent explicit-size constraints, improving cache reuse when parent constraints vary but the control's desired size is fixed by explicit width and/or height
- `[~]` `VisualElement` now also quantizes finite measure-cache constraints to reduce misses caused only by tiny floating-point drift between otherwise equivalent layout passes
- `[~]` `Editor.ContentWidth` now reuses the multiline line-width cache from `TextBox` instead of remeasuring every source line to drive horizontal scrolling and scrollbar sizing
- `[x]` Performance telemetry now exposes measure-cache hits and misses per frame, so the DevTool can show whether the richer measure cache is paying off in real workloads
- `[x]` Performance telemetry now also exposes a per-frame measure-cache hit rate, making the richer measure cache easier to evaluate quickly in the DevTool
