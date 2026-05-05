# Signals Best Practices

## Overview

Rayo uses signals for mutable state, computed state, and effects:

- `Signal<T>` for mutable state
- `Computed<T>` for derived state
- `Effect` for side effects
- `SignalList<T>` for collection state

## Prefer Signals for State

```csharp
private readonly Signal<string> _query = new("");
private readonly Signal<bool> _isBusy = new(false);
```

## Prefer Computed for Derived Values

```csharp
private readonly Computed<bool> _canSave;

public EditorView()
{
    _canSave = new Computed<bool>(() => _title.Value.Length > 0 && !_isBusy.Value);
    RegisterDisposable(_canSave);
}
```

## Prefer Effects for Imperative Reactions

```csharp
RegisterDisposable(new Effect(() =>
{
    Logger.Write($"Search query changed: {_query.Value}");
}));
```

## Defer Structural UI Work

If a signal change leads to tree mutations, defer that work:

```csharp
RegisterDisposable(_items.Subscribe(() =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
}));
```

## Use Hook APIs Inside Build

Inside `Build()`, prefer:

- `Hooks.UseSignal(...)`
- `Hooks.UseComputed(...)`
- `Hooks.UseEffect(...)`

That keeps state stable across rebuilds and hot reload.
