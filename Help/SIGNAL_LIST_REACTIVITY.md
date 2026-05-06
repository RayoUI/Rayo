# SignalList Reactivity

## Overview

`SignalList<T>` is the collection primitive for signal-driven UI state. It exposes list semantics plus signal semantics:

- list mutations such as `Add`, `Remove`, `Clear`, and index assignment
- dependency tracking through `Value`, `Count`, enumeration, and index access
- typed change subscriptions when you need structural details

## Common Patterns

### Derived count

```csharp
private readonly SignalList<TodoItem> _items = new();
private readonly Computed<int> _pendingCount;

public TodoView()
{
    _pendingCount = new Computed<int>(() => _items.Count(item => !item.Done));
    RegisterDisposable(_pendingCount);
}
```

### Binding a control property

```csharp
new TreeView()
    .Items(_items);
```

### Listening for structural changes

```csharp
RegisterDisposable(_items.Subscribe(change =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
}));
```

## Structural UI Updates

Signal list notifications are synchronous. If a change leads to tree mutations such as `ClearChildren()`, `AddChild()`, or `Rebuild()`, defer the work with `UIUpdateQueue.EnqueueUIUpdate(...)`.
