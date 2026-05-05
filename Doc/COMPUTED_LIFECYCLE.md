# Computed Lifecycle

## Overview

`Computed<T>` is a derived signal. It tracks the signals it reads, subscribes to them automatically, and re-runs when any dependency changes.

## Rules

1. Create long-lived computed values once per component or view model.
2. Dispose computed values when their owner goes away if you create them outside of `Hooks`.
3. Prefer `Hooks.UseComputed(...)` inside `Build()` when the computed belongs to hook state.

## Recommended Patterns

### Field-owned computed

```csharp
public class TodoHeader : UserControl
{
    private readonly SignalList<TodoItem> _items = new();
    private readonly Computed<int> _pendingCount;

    public TodoHeader()
    {
        _pendingCount = new Computed<int>(() => _items.Count(item => !item.Done));
        RegisterDisposable(_pendingCount);
    }
}
```

### Hook-owned computed

```csharp
public override VisualElement Build()
{
    var count = Hooks.UseSignal(0);
    var label = Hooks.UseComputed(() => $"Count: {count.Value}");

    return new Label().Text(label);
}
```

## Why This Matters

`Computed<T>` holds subscriptions to its dependencies. Giving it a clear owner keeps the dependency graph stable and prevents orphaned subscriptions.
