# FluentExtensions Reference

Monadic fluent extensions for declarative UI composition. All methods live in `Rayo.Reactivity.FluentExtensions` and extend `VisualElement` (or `CompositeView<T>` for collection operations), so they are available on every built-in and custom control via `using Rayo.Reactivity;`.

Every method returns the original element (`this`) so calls can be chained freely.

## Table of Contents

1. [Conditional operations](#1-conditional-operations)
2. [Null safety](#2-null-safety)
3. [Mapping and transformation](#3-mapping-and-transformation)
4. [Signal binding](#4-signal-binding)
5. [Collection operations](#5-collection-operations)
6. [State and caching](#6-state-and-caching)
7. [Validation and guards](#7-validation-and-guards)
8. [Composition helpers](#8-composition-helpers)
9. [Quick API reference](#9-quick-api-reference)

## 1. Conditional operations

### 1.1 When

Applies an action when a condition is `true`. An optional `onFalse` action runs when the condition is `false`.

```csharp
new Label()
    .When(isAdmin, l => l.Text("Admin Panel"));

new Button()
    .When(isLoggedIn,
        onTrue: b => b.Text("Log out"),
        onFalse: b => b.Text("Log in"));
```

Reactive overload:

```csharp
var isOnline = new Signal<bool>(false);

new Label()
    .When(isOnline,
        onTrue: l => l.Text("Online").Foreground(Color.Green),
        onFalse: l => l.Text("Offline").Foreground(Color.Red));
```

Subscriptions are registered on the element via `RegisterDisposable` and cleaned up automatically when the element is disposed.

### 1.2 Unless

Inverse of `When`. Applies an action only when the condition is `false`.

```csharp
new Button()
    .Unless(isDisabled, b => b.Background(Color.Blue));

var isBusy = new Signal<bool>(false);

new Button()
    .Unless(isBusy, b => b.IsEnabled(true));
```

### 1.3 WhenAll

Applies an action only when all conditions are `true`.

```csharp
new Button()
    .WhenAll(b => b.IsEnabled(true), hasName, hasEmail, hasPassword);

var hasName = new Signal<bool>(false);
var hasEmail = new Signal<bool>(false);
var hasPassword = new Signal<bool>(false);

new Button()
    .WhenAll(
        onAllTrue: b => b.IsEnabled(true),
        onAnyFalse: b => b.IsEnabled(false),
        hasName, hasEmail, hasPassword);
```

### 1.4 WhenAny

Applies an action when at least one condition is `true`.

```csharp
new Panel()
    .WhenAny(p => p.Background(Color.Yellow), hasWarning, hasError);

var hasWarning = new Signal<bool>(false);
var hasError = new Signal<bool>(false);

new Panel()
    .WhenAny(
        onAnyTrue: p => p.Background(Color.Yellow),
        onAllFalse: p => p.Background(Color.Transparent),
        hasWarning, hasError);
```

## 2. Null safety

### 2.1 WhenNotNull

Applies an action or transformation only when a value is non-null.

```csharp
new Label()
    .WhenNotNull(username, (l, name) => l.Text($"Hello, {name}!"));

var selectedItem = new Signal<Item?>(null);

new Label()
    .WhenNotNull(selectedItem, (l, item) => l.Text(item.Name));
```

### 2.2 WhenNull

Applies an action only when the value is null.

```csharp
new Label()
    .WhenNull(error, l => l.Text("No errors found").Foreground(Color.Green));
```

### 2.3 OrElse

Returns the element itself if non-null, otherwise returns a fallback.

```csharp
var button = maybeButton.OrElse(new Button().Text("Default"));
var lazyButton = maybeButton.OrElse(() => CreateDefaultButton());
```

## 3. Mapping and transformation

### 3.1 Map

Transforms an element to a different element type.

```csharp
var label = new Label()
    .Text("Hello")
    .Map(l => new Panel().AddChild(l));
```

### 3.2 Bind (transformation)

Equivalent to `Map`.

```csharp
var decorated = new Button()
    .Text("Click me")
    .Bind(b => WrapInCard(b));
```

### 3.3 Tap

Applies a side effect without modifying the element.

```csharp
new Button()
    .Text("Submit")
    .Tap(b => Console.WriteLine($"Button id={b.Id} created"))
    .Background(Color.Blue);
```

### 3.4 Apply

Applies multiple independent actions in sequence.

```csharp
new Button()
    .Text("Save")
    .Apply(AddShadow, AddBorder, SetFont);
```

### 3.5 Pipe

Passes the element through a sequence of transformation functions.

```csharp
new Button()
    .Text("Action")
    .Pipe(withRipple, withShadow, withPadding);
```

## 4. Signal binding

Signal overloads subscribe automatically, register the subscription on the element, call the action once immediately with the current value, and then re-run it on every change.

### 4.1 Bind

```csharp
var count = new Signal<int>(0);

new Label()
    .Bind(count, (l, n) => l.Text($"Count: {n}"));
```

### 4.2 Observe

```csharp
var theme = new Signal<string>("light");

new Panel()
    .Observe(theme, (p, t) =>
        p.Background(t == "dark" ? Color.Black : Color.White));
```

### 4.3 React

```csharp
var saveTriggered = new Signal<bool>(false);

new Label()
    .React(saveTriggered, (l, saved) =>
    {
        if (saved)
        {
            l.Text("Saved!").Foreground(Color.Green);
        }
    });
```

## 5. Collection operations

These methods are only available on `CompositeView<T>`.

### 5.1 ForEachChild

```csharp
new VStack()
    .ForEachChild(child => child.Opacity(0.5f));
```

### 5.2 ForEachChildOfType

```csharp
new VStack()
    .ForEachChildOfType<VStack, Button>(btn => btn.IsEnabled(false));
```

### 5.3 WithChildren

```csharp
new VStack()
    .WithChildren(items.Select(name => new Label().Text(name)));
```

### 5.4 WithChildrenWhen

```csharp
new VStack()
    .WithChildrenWhen(showActions,
        new Button().Text("Edit"),
        new Button().Text("Delete"));
```

## 6. State and caching

### 6.1 Once

```csharp
bool initialized = false;

new Panel()
    .Once(p => p.Background(Color.White), ref initialized);
```

### 6.2 WithState

```csharp
new Panel()
    .WithState(config, (p, cfg) =>
        p.Background(cfg.Color).BorderRadius(cfg.Radius));
```

## 7. Validation and guards

### 7.1 Guard

```csharp
new Button()
    .Guard(label != null, "Button label must not be null")
    .Text(label!);
```

### 7.2 Validate

```csharp
new TextBox()
    .Validate(tb => tb.MaxLength > 0, "MaxLength must be positive");
```

### 7.3 TryApply

```csharp
new Image()
    .TryApply(
        img => img.Source(LoadImageFromDisk()),
        ex => Console.WriteLine($"Image load failed: {ex.Message}"));
```

## 8. Composition helpers

### 8.1 Compose

```csharp
Func<Button, Panel> wrapInCard = b => new Panel().AddChild(b);
Func<Panel, Frame> wrapInFrame = p => new Frame().AddChild(p);

Func<Button, Frame> wrapInCardAndFrame = wrapInCard.Compose(wrapInFrame);
```

### 8.2 Chain

```csharp
new Button()
    .Text("Submit")
    .Chain(ConfigureSubmitButton)
    .OnTap(() => Submit());
```

## 9. Quick API reference

### Conditional operations

| Method | Signature | Description |
|---|---|---|
| `When` | `(bool, Action<T>, Action<T>?)` | Apply action when condition is true |
| `When` | `(IReadableSignal<bool>, Action<T>, Action<T>?)` | Signal version of `When` |
| `Unless` | `(bool, Action<T>)` | Apply action when condition is false |
| `Unless` | `(IReadableSignal<bool>, Action<T>)` | Signal version of `Unless` |
| `WhenAll` | `(Action<T>, params bool[])` | Apply when all booleans are true |
| `WhenAll` | `(Action<T>, Action<T>?, params IReadableSignal<bool>[])` | Signal version of `WhenAll` |
| `WhenAny` | `(Action<T>, params bool[])` | Apply when any boolean is true |
| `WhenAny` | `(Action<T>, Action<T>?, params IReadableSignal<bool>[])` | Signal version of `WhenAny` |

### Null safety

| Method | Signature | Description |
|---|---|---|
| `WhenNotNull` | `(TValue?, Func<T, TValue, T>)` | Transform when value is non-null |
| `WhenNotNull` | `(TValue?, Action<T, TValue>)` | Act when value is non-null |
| `WhenNotNull` | `(IReadableSignal<TValue?>, Func<T, TValue, T>)` | Signal version |
| `WhenNull` | `(TValue?, Action<T>)` | Act when value is null |
| `OrElse` | `(T)` | Return self or a default element |
| `OrElse` | `(Func<T>)` | Return self or a factory-created element |

### Mapping and transformation

| Method | Signature | Description |
|---|---|---|
| `Map` | `(Func<T, TResult>)` | Transform element to a different type |
| `Bind` | `(Func<T, TResult>)` | Alias for `Map` |
| `Tap` | `(Action<T>)` | Side effect; returns original element |
| `Apply` | `(params Action<T>[])` | Apply multiple actions in sequence |
| `Pipe` | `(params Func<T, T>[])` | Chain transformation functions |

### Signal binding

| Method | Signature | Description |
|---|---|---|
| `Bind` | `(IReadableSignal<TValue>, Action<T, TValue>)` | Bind signal; call immediately and on change |
| `Observe` | `(IReadableSignal<TValue>, Func<T, TValue, T>)` | Observe and transform; call immediately and on change |
| `React` | `(IReadableSignal<TValue>, Action<T, TValue>)` | React to signal; skips initial value |

### Collection operations (`CompositeView<T>` only)

| Method | Signature | Description |
|---|---|---|
| `ForEachChild` | `(Action<VisualElement>)` | Apply action to all children |
| `ForEachChildOfType<TChild>` | `(Action<TChild>)` | Apply action to children of type `TChild` |
| `WithChildren` | `(IEnumerable<VisualElement>)` | Add children from an enumerable |
| `WithChildrenWhen` | `(bool, params VisualElement[])` | Add children only when condition is true |

### State and caching

| Method | Signature | Description |
|---|---|---|
| `Once` | `(Action<T>, ref bool)` | Execute action at most once |
| `WithState` | `(TState, Action<T, TState>)` | Pass external state into an action |

### Validation and guards

| Method | Signature | Description |
|---|---|---|
| `Guard` | `(bool, string?)` | Throw if condition is false |
| `Validate` | `(Func<T, bool>, string?)` | Throw if validator returns false |
| `TryApply` | `(Action<T>, Action<Exception>?)` | Apply action; catch and forward exceptions |

### Composition helpers

| Method | Signature | Description |
|---|---|---|
| `Compose` | `(Func<T, TIntermediate>, Func<TIntermediate, TResult>)` | Compose two functions into one |
| `Chain` | `(Action<T>)` | Apply a named delegate inline |

Source: `Rayo/Reactivity/FluentExtensions.cs`
