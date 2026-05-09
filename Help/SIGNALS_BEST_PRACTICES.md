# Signals Best Practices

For a complete usage guide with practical examples, see `Help/SIGNALS_GUIDE.md`.

## Overview

Rayo uses signals for mutable state, computed state, and effects:

- `Signal<T>` for mutable state
- `Computed<T>` for derived state
- `Effect` for side effects
- `SignalList<T>` for collection state

## Prefer Signals for State

```csharp
private readonly Signal<string> _query;
private readonly Signal<bool> _isBusy;

public EditorView()
{
    _query = UseSignal(string.Empty);
    _isBusy = UseSignal(false);
}
```

## Prefer Computed for Derived Values

```csharp
private readonly Computed<bool> _canSave;

public EditorView()
{
    _canSave = this.UseComputed(() => _title.Value.Length > 0 && !_isBusy.Value);
}
```

## Prefer Effects for Imperative Reactions

```csharp
this.UseEffect(() =>
{
    Logger.Write($"Search query changed: {_query.Value}");
}));
```

## Defer Structural UI Work

If a signal change leads to tree mutations, defer that work:

```csharp
this.UseSubscription(_items, () =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
}));
```

## Prefer Lifecycle-Owned Helpers Outside Hooks

When code runs outside `Build()` hooks, prefer lifecycle-owned methods on `IReactiveOwner` over manual `RegisterDisposable(...)` boilerplate.

```csharp
private readonly Computed<bool> _canSave;

public EditorView()
{
    _canSave = this.UseComputed(() => !string.IsNullOrWhiteSpace(_title.Value));

    this.UseEffect(() => Logger.Write($"Busy: {_isBusy.Value}"));

    this.UseSubscription(_query, value =>
    {
        Logger.Write($"Query changed: {value}");
    });
}
```

Available lifecycle-owned methods:

- `Use(...)`
- `UseSignal(...)`
- `UseSignalList(...)`
- `UseComputed(...)`
- `UseEffect(...)`
- `UseSubscription(...)`

## Use Hook APIs Inside Build

Inside `Build()`, prefer:

- `Hooks.UseSignal(...)`
- `Hooks.UseComputed(...)`
- `Hooks.UseEffect(...)`

That keeps state stable across rebuilds and hot reload.
