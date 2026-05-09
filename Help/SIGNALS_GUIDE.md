# Rayo Signals Guide

## Overview

Rayo uses a signals-first reactive model for UI state and derived values.

Core types:

- `Signal<T>` for mutable state
- `Computed<T>` for derived read-only state
- `Effect` for imperative reactions
- `SignalList<T>` for collection state

Use signals for state, computed values for derivations, effects for side effects, and `SignalList<T>` when the state is a mutable collection.

## The two ownership models

Rayo supports two valid ownership models.

### 1. Hooks inside `Build()`

Use hooks when the state is local to a `Build()` execution and should survive rebuilds.

- `Hooks.UseSignal(...)`
- `Hooks.UseComputed(...)`
- `Hooks.UseEffect(...)`

This is the preferred pattern for local view state created inside `Build()`.

### 2. `IReactiveOwner` outside `Build()`

Use `IReactiveOwner` methods when the state belongs to the owner itself and is created in a constructor, `OnInit`, `OnInitialized`, or any method outside hooks.

Available methods:

- `Use(...)`
- `UseSignal(...)`
- `UseSignalList(...)`
- `UseComputed(...)`
- `UseEffect(...)`
- `UseSubscription(...)`

`VisualElement` and `ViewModelBase` implement `IReactiveOwner`.

## Best-practice rule

- Inside `Build()`: prefer hooks.
- Outside `Build()`: prefer `IReactiveOwner` methods.
- Avoid creating `new Signal<T>(...)`, `new Computed<T>(...)`, or `new SignalList<T>(...)` directly inside `Build()` unless you intentionally want transient state.

## When to use each reactive type

### `Signal<T>`

Use when:

- the value changes over time,
- the value is owned by a control or view model,
- other UI or logic needs to react to it.

Typical examples:

- search query,
- selected tab,
- loading flag,
- current zoom,
- dialog visibility.

### `Computed<T>`

Use when:

- the value can be derived from other signals,
- you want to avoid duplicated state,
- the value should stay consistent automatically.

Typical examples:

- `CanSave`,
- filtered item lists,
- dynamic titles,
- visibility flags.

### `Effect`

Use when:

- you need imperative code to run when signals change,
- you are logging, persisting, or calling external services,
- you need to trigger non-UI work from reactive state.

### `SignalList<T>`

Use when:

- the state is a list,
- the list changes incrementally,
- UI or computed values depend on count or contents.

## `Signal<T>` basics

### Plain construction

```csharp
private readonly Signal<string> _query = new(string.Empty);
private readonly Signal<bool> _isBusy = new(false);
```

### Lifecycle-owned construction

```csharp
private readonly Signal<string> _query;
private readonly Signal<bool> _isBusy;

public SearchView()
{
    _query = UseSignal(string.Empty);
    _isBusy = UseSignal(false);
}
```

Read and write through `.Value`:

```csharp
_query.Value = "rayo";
_isBusy.Value = true;

string currentQuery = _query.Value;
bool busy = _isBusy.Value;
```

Signals notify subscribers only when the value actually changes according to `EqualityComparer<T>.Default`.

## `Computed<T>` basics

`Computed<T>` automatically tracks the signals it reads while evaluating its function.

```csharp
private readonly Signal<string> _title;
private readonly Signal<bool> _isSaving;
private readonly Computed<bool> _canSave;

public EditorViewModel()
{
    _title = UseSignal(string.Empty);
    _isSaving = UseSignal(false);
    _canSave = UseComputed(() =>
        !string.IsNullOrWhiteSpace(_title.Value) && !_isSaving.Value);
}
```

Read the current value through `.Value`:

```csharp
if (_canSave.Value)
{
    Save();
}
```

## `Effect` basics

`Effect` runs immediately and re-runs whenever any signal read during execution changes.

```csharp
private readonly Signal<string> _query;

public SearchViewModel()
{
    _query = UseSignal(string.Empty);

    UseEffect(() =>
    {
        Logger.Write($"Current query: {_query.Value}");
    });
}
```

Use effects for side effects, not as state containers.

## `SignalList<T>` basics

### Plain construction

```csharp
private readonly SignalList<string> _items = new();
```

### Lifecycle-owned construction

```csharp
private readonly SignalList<string> _items;

public ItemsViewModel()
{
    _items = UseSignalList<string>();
}
```

Seeded variant:

```csharp
private readonly SignalList<string> _items;

public ItemsViewModel()
{
    _items = UseSignalList(["One", "Two", "Three"]);
}
```

Use it like a normal mutable list:

```csharp
_items.Add("Four");
_items[0] = "Updated";
_items.Remove("Two");
_items.Clear();
```

Reactive reads include:

- `_items.Value`
- `_items.Count`
- indexed access
- enumeration

List mutations such as `Add`, `Remove`, `Clear`, and index assignment notify reactive dependents and change subscribers.

## Subscriptions

### Manual subscription

```csharp
IDisposable subscription = _query.Subscribe(value =>
{
    Console.WriteLine($"Query changed to: {value}");
});
```

### Lifecycle-owned subscription

```csharp
UseSubscription(_query, value =>
{
    Logger.Write($"Query changed: {value}");
});
```

For `SignalList<T>` you can subscribe to full-list updates:

```csharp
UseSubscription(_items, values =>
{
    Logger.Write($"Count: {values.Count}");
});
```

Or to change events:

```csharp
UseSubscription(_items, change =>
{
    Logger.Write($"Type: {change.Type}, Index: {change.Index}");
});
```

`SignalListChange<T>` exposes:

- `Type`
- `Index`
- `NewValue`
- `OldValue`

## Common `SignalList<T>` patterns

### Derived count

```csharp
private readonly SignalList<TodoItem> _items;
private readonly Computed<int> _pendingCount;

public TodoViewModel()
{
    _items = UseSignalList<TodoItem>();
    _pendingCount = UseComputed(() => _items.Count(item => !item.Done));
}
```

### Binding a control property

```csharp
new TreeView()
    .Items(_items);
```

### Listening for structural changes

```csharp
UseSubscription(_items, change =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
});
```

## Mapping and projections

Use `Map(...)` to derive a projection from any readable signal.

```csharp
var upperName = _name.Map(value => value.ToUpperInvariant());
```

Use `Computed<T>` when the derived value depends on multiple signals or when you want an explicitly owned field.

```csharp
_canSave = UseComputed(() =>
    !string.IsNullOrWhiteSpace(_title.Value) && !_isSaving.Value);
```

## Hooks inside `Build()`

When state is local to `Build()`, use hooks.

```csharp
public override VisualElement Build()
{
    var count = Hooks.UseSignal(0);
    var label = Hooks.UseComputed(() => $"Count: {count.Value}");

    Hooks.UseEffect(() =>
    {
        Console.WriteLine($"Counter changed to {count.Value}");
    }, count);

    return new VStack()
        .Spacing(12)
        .Children(
            new Label().Text(label.Value),
            new HStack()
                .Spacing(8)
                .Children(
                    new Button().Text("-").OnTapped(() => count.Value--),
                    new Button().Text("+").OnTapped(() => count.Value++)
                )
        );
}
```

### Why hooks are preferred inside `Build()`

`Build()` can run multiple times. Hooks keep state stable across rebuilds.

Avoid this pattern inside `Build()`:

```csharp
var count = new Signal<int>(0);
var label = new Computed<string>(() => $"Count: {count.Value}");
```

That recreates state every rebuild and usually leads to fragile behavior.

## `IReactiveOwner` outside `Build()`

Use owner methods when the state belongs to the control or view model instance.

### Example in a control

```csharp
public class EditorView : UserControl
{
    private readonly Signal<string> _title;
    private readonly Signal<bool> _isBusy;
    private readonly Computed<bool> _canSave;

    public EditorView()
    {
        _title = UseSignal(string.Empty);
        _isBusy = UseSignal(false);
        _canSave = UseComputed(() =>
            !string.IsNullOrWhiteSpace(_title.Value) && !_isBusy.Value);

        UseEffect(() =>
        {
            Console.WriteLine($"Can save changed: {_canSave.Value}");
        });

        UseSubscription(_title, value =>
        {
            Console.WriteLine($"Title: {value}");
        });
    }
}
```

### Example in a view model

```csharp
public class EditorViewModel : ViewModelBase
{
    private readonly Signal<string> _title;
    private readonly Signal<bool> _isBusy;
    private readonly Computed<bool> _canSave;

    public EditorViewModel()
    {
        _title = UseSignal(string.Empty);
        _isBusy = UseSignal(false);
        _canSave = UseComputed(() =>
            !string.IsNullOrWhiteSpace(_title.Value) && !_isBusy.Value);

        UseEffect(() =>
        {
            Logger.Write($"Can save: {_canSave.Value}");
        });

        UseSubscription(_title, value =>
        {
            Logger.Write($"Title changed: {value}");
        });
    }
}
```

## MVVM guidance

`ViewModelBase` implements `IReactiveOwner`, so view models can use:

- `UseSignal(...)`
- `UseSignalList(...)`
- `UseComputed(...)`
- `UseEffect(...)`
- `UseSubscription(...)`

This is the preferred reactive API for long-lived MVVM state.

## Structural UI updates must be deferred

If a signal change mutates the UI tree structure (`ClearChildren`, `AddChild`, `RemoveChild`, `Rebuild`), defer the work through `UIUpdateQueue.EnqueueUIUpdate(...)`.

Correct:

```csharp
UseSubscription(_items, () =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
});
```

Or batched by owner element:

```csharp
UseSubscription(_items, () =>
{
    UIUpdateQueue.EnqueueUIUpdate(this, Rebuild);
});
```

Avoid mutating UI structure synchronously from a signal callback.

## Binding UI properties to signals

Prefer signal-aware property overloads over manual subscriptions when available.

```csharp
var title = new Signal<string>("Welcome");
var isEnabled = new Signal<bool>(true);

var button = new Button()
    .Text(title)
    .IsEnabled(isEnabled);
```

This keeps the element reactive without extra plumbing.

## Common mistakes to avoid

### 1. Creating manual signals inside `Build()`

Prefer hooks there.

### 2. Duplicating derived state

Avoid storing values that can be derived from other signals. Prefer `Computed<T>`.

### 3. Using effects as state storage

Effects are for reactions, not for holding data.

### 4. Forgetting ownership outside hooks

If a `Computed<T>`, `Effect`, or subscription is long-lived and created outside hooks, give it a clear owner.

### 5. Mutating UI structure immediately from subscriptions

Use `UIUpdateQueue.EnqueueUIUpdate(...)`.

## Practical patterns

### Form validation

```csharp
var name = UseSignal(string.Empty);
var email = UseSignal(string.Empty);
var canSubmit = UseComputed(() =>
    !string.IsNullOrWhiteSpace(name.Value) &&
    email.Value.Contains('@'));
```

### Busy state

```csharp
var isBusy = UseSignal(false);

var button = new Button()
    .Text("Save")
    .IsEnabled(isBusy.Map(x => !x));
```

### Empty state from `SignalList<T>`

```csharp
var notifications = UseSignalList<string>();
var isEmpty = UseComputed(() => notifications.Count == 0);
```

### Selection state

```csharp
var selectedIndex = UseSignal(-1);
var hasSelection = UseComputed(() => selectedIndex.Value >= 0);
```

## Summary

- Use hooks for state created inside `Build()`.
- Use `IReactiveOwner` methods for state created outside hooks.
- Use `Signal<T>` for mutable state.
- Use `SignalList<T>` for mutable collections.
- Use `Computed<T>` for derived state.
- Use `Effect` for side effects.
- Prefer signal-aware UI bindings where available.
- Defer structural UI mutations through `UIUpdateQueue.EnqueueUIUpdate(...)`.
new Label().Text(nameSignal);
```

Over this:

```csharp
var label = new Label();
var subscription = nameSignal.Subscribe(value => label.Text(value));
label.RegisterDisposable(subscription);
```

Manual subscriptions are still useful when the target API is not signal-aware.

## Structural UI updates must be deferred

If a signal change adds, removes, clears, or rebuilds UI elements, do not mutate the tree directly from the subscription or effect.

Use `UIUpdateQueue.EnqueueUIUpdate(...)`.

### Correct

```csharp
Subscribe(_items, () =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
}));
```

Or batch by element:

```csharp
Subscribe(_items, () =>
{
    UIUpdateQueue.EnqueueUIUpdate(this, Rebuild);
}));
```

### Avoid

```csharp
RegisterDisposable(_items.Subscribe(() =>
{
    ClearChildren();
    foreach (var item in _items)
    {
        AddChild(new Label(item));
    }
}));
```

The deferred approach keeps structural mutations aligned with the UI update cycle.

## Complete example: reactive todo panel

```csharp
public sealed record TodoItem(string Title, bool IsDone);

public class TodoPanel : UserControl
{
    public override VisualElement Build()
    {
        var draft = Hooks.UseSignal(string.Empty);
        var todos = Hooks.UseSignal(new SignalList<TodoItem>());
        var total = Hooks.UseComputed(() => todos.Value.Count);
        var completed = Hooks.UseComputed(() => todos.Value.Count(item => item.IsDone));
        var canAdd = Hooks.UseComputed(() => !string.IsNullOrWhiteSpace(draft.Value));

        Hooks.UseEffect(() =>
        {
            Console.WriteLine($"Todos changed. Total: {total.Value}, Completed: {completed.Value}");
        }, total, completed);

        void AddTodo()
        {
            if (!canAdd.Value)
            {
                return;
            }

            todos.Value.Add(new TodoItem(draft.Value.Trim(), false));
            draft.Value = string.Empty;
        }

        return new VStack()
            .Spacing(12)
            .Children(
                new Entry()
                    .Placeholder("Add a task")
                    .Text(draft),
                new Button()
                    .Text("Add")
                    .IsEnabled(canAdd)
                    .OnTapped(AddTodo),
                new Label().Text($"Total: {total.Value}"),
                new Label().Text($"Completed: {completed.Value}"),
                new Label().Text(string.Join(", ", todos.Value.Select(x => x.Title)))
            );
    }
}
```

## Lifetime guidance

### In a `UserControl`

- prefer hooks inside `Build()` for local state,
- prefer `UseComputed(...)`, `UseEffect(...)`, and `UseSubscription(...)` over manual registration,
- if you still create `Computed<T>` or `Effect` manually, register them for disposal,
- if you subscribe manually, register the returned `IDisposable`.

```csharp
this.UseSubscription(_status, _ => MarkNeedsPaint());
this.UseEffect(() => Logger.Write(_status.Value));
```

### In a view model or service

- keep explicit ownership,
- dispose subscriptions, computed values, and effects when the owner is disposed.

## Common mistakes to avoid

### 1. Duplicating derived state

Avoid:

```csharp
private readonly Signal<bool> _canSave = new(false);
```

When it can be derived from other state. Prefer:

```csharp
private readonly Computed<bool> _canSave;
```

### 2. Using effects as state containers

Effects are for reactions, not for storing data.

### 3. Forgetting disposal outside hooks

If an object is not hook-owned and not registered for disposal, it can keep reacting longer than intended.

### 4. Mutating UI structure synchronously from a signal callback

Use `UIUpdateQueue.EnqueueUIUpdate(...)` for structural changes.

### 5. Overusing manual subscriptions for property binding

Prefer signal-aware property overloads when available.

## Practical patterns

### Form validation

```csharp
var name = new Signal<string>(string.Empty);
var email = new Signal<string>(string.Empty);
var canSubmit = new Computed<bool>(() =>
    !string.IsNullOrWhiteSpace(name.Value) &&
    email.Value.Contains('@'));
```

### Busy state

```csharp
var isBusy = new Signal<bool>(false);

var button = new Button()
    .Text("Save")
    .IsEnabled(isBusy.Map(x => !x));
```

### Empty state from `SignalList<T>`

```csharp
var notifications = new SignalList<string>();
var isEmpty = notifications.Map(items => items.Count == 0);
```

### Selection state

```csharp
var selectedIndex = new Signal<int>(-1);
var hasSelection = selectedIndex.Map(index => index >= 0);
```

## Summary

- Use `Signal<T>` for mutable state.
- Use `Computed<T>` for derived state.
- Use `Effect` for side effects.
- Use `SignalList<T>` for reactive collections.
- Prefer signal-aware UI bindings over manual subscriptions.
- Use hooks inside `Build()` for local component state.
- Defer structural UI mutations through `UIUpdateQueue.EnqueueUIUpdate(...)`.
