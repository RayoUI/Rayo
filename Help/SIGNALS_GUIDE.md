# Rayo Signals Guide

## Overview

Rayo uses a signals-first reactive model for UI state and derived values.

Core types:

- `Signal<T>`: mutable state.
- `Computed<T>`: derived read-only state.
- `Effect`: imperative reactions to signal changes.
- `SignalList<T>`: reactive collection state.

Use signals for local component state, computed values for projections, effects for side effects, and `SignalList<T>` when the state is a collection.

## When to use each type

### Use `Signal<T>` when

- the value changes over time,
- the value is owned by a component or view model,
- other UI or logic needs to react to updates.

Examples:

- current search text,
- selected tab index,
- loading flag,
- current zoom level.

### Use `Computed<T>` when

- the value can be derived from one or more signals,
- you want to avoid duplicating state,
- the derived value should stay consistent automatically.

Examples:

- `CanSave` from title validity and loading state,
- filtered items from query and source list,
- formatted labels derived from raw values.

### Use `Effect` when

- you need to run imperative code when signals change,
- you are logging, persisting, sending analytics, or triggering non-UI work,
- you need to bridge reactive state to an API that is not signal-aware.

Examples:

- write to console or logger,
- save user preferences,
- trigger a service call,
- enqueue UI structure updates.

### Use `SignalList<T>` when

- the state is a list that changes incrementally,
- you want collection updates to notify subscribers,
- computed values depend on the list contents or count.

Examples:

- task lists,
- notifications,
- search results,
- selected files.

## `Signal<T>` basics

Create a signal with an initial value:

```csharp
private readonly Signal<string> _query = new(string.Empty);
private readonly Signal<bool> _isBusy = new(false);
private readonly Signal<int> _page = new(1);
```

Read and write through `.Value`:

```csharp
_query.Value = "rayo";
_isBusy.Value = true;

string currentQuery = _query.Value;
bool busy = _isBusy.Value;
```

Changes only notify subscribers when the new value is different from the current value according to `EqualityComparer<T>.Default`.

## Subscribing to a signal

You can subscribe with a typed callback:

```csharp
IDisposable subscription = _query.Subscribe(value =>
{
    Console.WriteLine($"Query changed to: {value}");
});
```

Or with a parameterless callback:

```csharp
IDisposable subscription = _query.Subscribe(() =>
{
    Console.WriteLine("Query changed");
});
```

Dispose subscriptions when the owner goes away unless the subscription is registered in a component-managed lifetime.

## Mapping a signal

Use `Map(...)` to create a computed projection:

```csharp
private readonly Signal<string> _name = new("rayo");
private readonly Computed<string> _upperName;

public MyView()
{
    _upperName = _name.Map(name => name.ToUpperInvariant());
    RegisterDisposable(_upperName);
}
```

The same projection is available through the `IReadableSignal<T>` extension method.

## `Computed<T>` basics

`Computed<T>` automatically tracks the signals it reads while evaluating its function.

```csharp
private readonly Signal<string> _title = new(string.Empty);
private readonly Signal<bool> _isSaving = new(false);
private readonly Computed<bool> _canSave;

public EditorView()
{
    _canSave = new Computed<bool>(() =>
        !string.IsNullOrWhiteSpace(_title.Value) && !_isSaving.Value);

    RegisterDisposable(_canSave);
}
```

Use `.Value` to read the current derived result:

```csharp
if (_canSave.Value)
{
    Save();
}
```

## Dependency tracking in computed values

Dependencies are discovered automatically from signal reads inside the compute function.

```csharp
private readonly Signal<int> _count = new(0);
private readonly Signal<int> _step = new(2);

private readonly Computed<int> _nextValue;

public CounterView()
{
    _nextValue = new Computed<int>(() => _count.Value + _step.Value);
    RegisterDisposable(_nextValue);
}
```

When `_count` or `_step` changes, `_nextValue` becomes dirty, recomputes, and notifies its subscribers.

## Subscribing to a computed value

```csharp
RegisterDisposable(_canSave.Subscribe(value =>
{
    Console.WriteLine($"Can save: {value}");
}));
```

`Computed<T>` is disposable. Dispose it when it is not owned by hooks or component lifetime management.

## `Effect` basics

`Effect` runs immediately and then re-runs whenever any signal read during execution changes.

```csharp
private readonly Signal<string> _query = new(string.Empty);

public SearchView()
{
    RegisterDisposable(new Effect(() =>
    {
        Console.WriteLine($"Current query: {_query.Value}");
    }));
}
```

This is appropriate for side effects, not for representing state.

## Good effect usage

### Logging

```csharp
RegisterDisposable(new Effect(() =>
{
    Logger.Write($"Counter value: {_counter.Value}");
}));
```

### Persisting preferences

```csharp
RegisterDisposable(new Effect(() =>
{
    Preferences.Set("theme", _theme.Value);
}));
```

### Bridging state to an async workflow trigger

```csharp
RegisterDisposable(new Effect(() =>
{
    if (_reloadRequested.Value)
    {
        _ = ReloadAsync();
    }
}));
```

If the effect changes UI tree structure, defer that structural work through `UIUpdateQueue`.

## `SignalList<T>` basics

Create a reactive list:

```csharp
private readonly SignalList<string> _items = new();
```

Or seed it from existing data:

```csharp
private readonly SignalList<string> _items = new(["One", "Two", "Three"]);
```

Use it like a normal mutable list:

```csharp
_items.Add("Four");
_items[0] = "Updated";
_items.Remove("Two");
_items.Clear();
```

Read reactive collection state through:

- `_items.Value` for the full read-only list,
- `_items.Count` for the item count,
- enumeration or indexed access.

These reads participate in dependency tracking.

## Subscribing to `SignalList<T>`

### Subscribe to the full collection

```csharp
RegisterDisposable(_items.Subscribe(values =>
{
    Console.WriteLine($"Item count: {values.Count}");
}));
```

### Subscribe to individual list changes

```csharp
RegisterDisposable(_items.Subscribe(change =>
{
    Console.WriteLine($"Type: {change.Type}, Index: {change.Index}");
}));
```

`SignalListChange<T>` exposes:

- `Type`
- `Index`
- `NewValue`
- `OldValue`

## Derived values from `SignalList<T>`

Use `Computed<T>` or `Map(...)` when the UI depends on aggregate values.

```csharp
private readonly SignalList<TaskItem> _tasks = new();
private readonly Computed<int> _completedCount;
private readonly Computed<bool> _hasTasks;

public TasksView()
{
    _completedCount = new Computed<int>(() => _tasks.Count(task => task.IsDone));
    _hasTasks = _tasks.Map(items => items.Count > 0);

    RegisterDisposable(_completedCount);
    RegisterDisposable(_hasTasks);
}
```

## Using hooks inside `Build()`

Inside `UserControl.Build()`, prefer hook ownership for local reactive state.

Available hooks:

- `Hooks.UseSignal(...)`
- `Hooks.UseComputed(...)`
- `Hooks.UseEffect(...)`

`UserControl` already calls `Hooks.Begin(this)` before `Build()`, so hooks are safe there.

### Example: counter with hooks

```csharp
public class CounterCard : UserControl
{
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
}
```

### Example: filter UI with hooks

```csharp
public class SearchPanel : UserControl
{
    public override VisualElement Build()
    {
        var query = Hooks.UseSignal(string.Empty);
        var items = Hooks.UseSignal(new[] { "Button", "Label", "ComboBox", "ListView" });
        var filtered = Hooks.UseComputed(() =>
            items.Value
                .Where(x => x.Contains(query.Value, StringComparison.OrdinalIgnoreCase))
                .ToArray());

        return new VStack()
            .Spacing(10)
            .Children(
                new Entry()
                    .Placeholder("Search controls")
                    .Text(query),
                new Label().Text($"Matches: {filtered.Value.Length}"),
                new Label().Text(string.Join(", ", filtered.Value))
            );
    }
}
```

## Binding UI properties to signals

Prefer property overloads that accept `IReadableSignal<T>` instead of manual subscriptions.

```csharp
var title = new Signal<string>("Welcome");
var isEnabled = new Signal<bool>(true);

var button = new Button()
    .Text(title)
    .IsEnabled(isEnabled);
```

This keeps the element reactive without custom subscription plumbing.

## Manual subscriptions vs signal-aware property binding

Prefer this:

```csharp
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
RegisterDisposable(_items.Subscribe(() =>
{
    UIUpdateQueue.EnqueueUIUpdate(RebuildRows);
}));
```

Or batch by element:

```csharp
RegisterDisposable(_items.Subscribe(() =>
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
- if you create `Computed<T>` or `Effect` manually, register them for disposal,
- if you subscribe manually, register the returned `IDisposable`.

```csharp
RegisterDisposable(_status.Subscribe(_ => MarkNeedsPaint()));
RegisterDisposable(_canSave);
RegisterDisposable(new Effect(() => Logger.Write(_status.Value)));
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
