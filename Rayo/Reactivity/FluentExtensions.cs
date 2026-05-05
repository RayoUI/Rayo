using Rayo.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rayo.Reactivity;

/// <summary>
/// Monadic fluent extensions for declarative UI composition
/// </summary>
public static class FluentExtensions
{
    #region Conditional Operations (Functor/Applicative)

    /// <summary>
    /// Conditionally applies transformations based on a boolean condition
    /// </summary>
    public static T When<T>(this T self, bool condition, Action<T> onTrue, Action<T>? onFalse = null) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(onTrue);

        if (condition)
            onTrue(self);
        else
            onFalse?.Invoke(self);

        return self;
    }

    /// <summary>
    /// Reactively applies transformations based on a boolean signal.
    /// </summary>
    public static T When<T>(this T self, IReadableSignal<bool> conditionSignal, Action<T> onTrue, Action<T>? onFalse = null) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(conditionSignal);
        global::System.ArgumentNullException.ThrowIfNull(onTrue);

        Action<bool> executeAction = value =>
        {
            if (value)
                onTrue(self);
            else
                onFalse?.Invoke(self);
        };

        var subscription = conditionSignal.Subscribe(executeAction);
        self.RegisterDisposable(subscription);
        executeAction(conditionSignal.Value);
        return self;
    }

    /// <summary>
    /// Applies transformation when condition is false (inverse of When)
    /// </summary>
    public static T Unless<T>(this T self, bool condition, Action<T> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        if (!condition)
            action(self);

        return self;
    }

    /// <summary>
    /// Reactively applies transformation when condition is false
    /// </summary>
    public static T Unless<T>(this T self, IReadableSignal<bool> conditionSignal, Action<T> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(conditionSignal);
        global::System.ArgumentNullException.ThrowIfNull(action);

        var subscription = conditionSignal.Subscribe(value =>
        {
            if (!value)
                action(self);
        });

        self.RegisterDisposable(subscription);
        if (!conditionSignal.Value)
            action(self);

        return self;
    }

    /// <summary>
    /// Applies transformation only when all conditions are true (static version)
    /// </summary>
    public static T WhenAll<T>(this T self, Action<T> action, params bool[] conditions) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);
        global::System.ArgumentNullException.ThrowIfNull(conditions);

        if (conditions.Length > 0 && global::System.Linq.Enumerable.All(conditions, c => c))
            action(self);

        return self;
    }

    /// <summary>
    /// Reactively applies transformation when all signal conditions are true.
    /// </summary>
    public static T WhenAll<T>(this T self, Action<T> onAllTrue, Action<T>? onAnyFalse = null, params IReadableSignal<bool>[] signals) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(onAllTrue);
        global::System.ArgumentNullException.ThrowIfNull(signals);

        if (signals.Length == 0)
            return self;

        // Track current state of all signals.
        var states = new bool[signals.Length];
        for (int i = 0; i < signals.Length; i++)
        {
            states[i] = signals[i].Value;
        }

        // Action to evaluate all conditions
        System.Action evaluateAll = () =>
        {
            bool allTrue = true;
            for (int i = 0; i < states.Length; i++)
            {
                if (!states[i])
                {
                    allTrue = false;
                    break;
                }
            }

            if (allTrue)
                onAllTrue(self);
            else
                onAnyFalse?.Invoke(self);
        };

        // Subscribe to each signal.
        for (int i = 0; i < signals.Length; i++)
        {
            int index = i;
            var subscription = signals[i].Subscribe(value =>
            {
                states[index] = value;
                evaluateAll();
            });
            self.RegisterDisposable(subscription);
        }

        // Initial evaluation
        evaluateAll();

        return self;
    }

    /// <summary>
    /// Applies transformation when any condition is true (static version)
    /// </summary>
    public static T WhenAny<T>(this T self, Action<T> action, params bool[] conditions) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);
        global::System.ArgumentNullException.ThrowIfNull(conditions);

        if (conditions.Length > 0 && global::System.Linq.Enumerable.Any(conditions, c => c))
            action(self);

        return self;
    }

    /// <summary>
    /// Reactively applies transformation when any signal condition is true.
    /// </summary>
    public static T WhenAny<T>(this T self, Action<T> onAnyTrue, Action<T>? onAllFalse = null, params IReadableSignal<bool>[] signals) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(onAnyTrue);
        global::System.ArgumentNullException.ThrowIfNull(signals);

        if (signals.Length == 0)
            return self;

        // Track current state of all signals.
        var states = new bool[signals.Length];
        for (int i = 0; i < signals.Length; i++)
        {
            states[i] = signals[i].Value;
        }

        // Action to evaluate any condition
        System.Action evaluateAny = () =>
        {
            bool anyTrue = false;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i])
                {
                    anyTrue = true;
                    break;
                }
            }

            if (anyTrue)
                onAnyTrue(self);
            else
                onAllFalse?.Invoke(self);
        };

        // Subscribe to each signal.
        for (int i = 0; i < signals.Length; i++)
        {
            int index = i;
            var subscription = signals[i].Subscribe(value =>
            {
                states[index] = value;
                evaluateAny();
            });
            self.RegisterDisposable(subscription);
        }

        // Initial evaluation
        evaluateAny();

        return self;
    }

    #endregion

    #region Null Safety (Maybe Monad)

    /// <summary>
    /// Applies transformation when value is not null
    /// </summary>
    public static T WhenNotNull<T, TValue>(this T self, TValue? value, Func<T, TValue, T> then) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(then);

        return value != null ? then(self, value) : self;
    }

    /// <summary>
    /// Reactively applies transformation when a signal value is not null.
    /// </summary>
    public static T WhenNotNull<T, TValue>(this T self, IReadableSignal<TValue?> signal, Func<T, TValue, T> then) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(signal);
        global::System.ArgumentNullException.ThrowIfNull(then);

        Action<TValue?> executeAction = value =>
        {
            if (value != null)
                then(self, value);
        };

        var subscription = signal.Subscribe(executeAction);
        self.RegisterDisposable(subscription);
        executeAction(signal.Value);
        return self;
    }

    /// <summary>
    /// Applies action when value is not null (simpler version without transformation)
    /// </summary>
    public static T WhenNotNull<T, TValue>(this T self, TValue? value, Action<T, TValue> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        if (value != null)
            action(self, value);

        return self;
    }

    /// <summary>
    /// Applies action when value is null (inverse)
    /// </summary>
    public static T WhenNull<T, TValue>(this T self, TValue? value, Action<T> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        if (value == null)
            action(self);

        return self;
    }

    /// <summary>
    /// Returns self if not null, otherwise returns default element
    /// </summary>
    public static T OrElse<T>(this T? self, T defaultValue) where T : VisualElement
    {
        return self ?? defaultValue;
    }

    /// <summary>
    /// Returns self if not null, otherwise invokes factory to create default
    /// </summary>
    public static T OrElse<T>(this T? self, Func<T> defaultFactory) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(defaultFactory);
        return self ?? defaultFactory();
    }

    #endregion

    #region Mapping and Transformation (Functor)

    /// <summary>
    /// Maps element through a transformation function (functor map)
    /// </summary>
    public static TResult Map<T, TResult>(this T self, Func<T, TResult> mapper) 
        where T : VisualElement 
        where TResult : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(mapper);

        return mapper(self);
    }

    /// <summary>
    /// Applies a transformation and returns the result
    /// </summary>
    public static TResult Bind<T, TResult>(this T self, Func<T, TResult> binder) 
        where T : VisualElement 
        where TResult : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(binder);

        return binder(self);
    }

    /// <summary>
    /// Applies a side effect and returns the original element (useful for debugging/logging)
    /// </summary>
    public static T Tap<T>(this T self, Action<T> sideEffect) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(sideEffect);

        sideEffect(self);
        return self;
    }

    /// <summary>
    /// Applies multiple actions in sequence
    /// </summary>
    public static T Apply<T>(this T self, params Action<T>[] actions) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(actions);

        foreach (var action in actions)
        {
            action?.Invoke(self);
        }

        return self;
    }

    /// <summary>
    /// Pipes element through a series of transformation functions
    /// </summary>
    public static T Pipe<T>(this T self, params Func<T, T>[] transformations) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(transformations);

        var result = self;
        foreach (var transform in transformations)
        {
            if (transform != null)
                result = transform(result);
        }

        return result;
    }

    #endregion

    #region Reactive Binding

    /// <summary>
    /// Binds a signal value to an action, executing on each change.
    /// </summary>
    public static T Bind<T, TValue>(this T self, IReadableSignal<TValue> signal, Action<T, TValue> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(signal);
        global::System.ArgumentNullException.ThrowIfNull(action);

        var subscription = signal.Subscribe(value => action(self, value));
        self.RegisterDisposable(subscription);
        action(self, signal.Value);

        return self;
    }

    /// <summary>
    /// Observes changes in a signal value and applies a transformation.
    /// </summary>
    public static T Observe<T, TValue>(this T self, IReadableSignal<TValue> signal, Func<T, TValue, T> transformer) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(signal);
        global::System.ArgumentNullException.ThrowIfNull(transformer);

        var subscription = signal.Subscribe(value => transformer(self, value));
        self.RegisterDisposable(subscription);
        transformer(self, signal.Value);

        return self;
    }

    /// <summary>
    /// Reacts to signal changes.
    /// </summary>
    public static T React<T, TValue>(this T self, IReadableSignal<TValue> signal, Action<T, TValue> reaction) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(signal);
        global::System.ArgumentNullException.ThrowIfNull(reaction);

        var subscription = signal.Subscribe(value => reaction(self, value));
        self.RegisterDisposable(subscription);

        return self;
    }

    #endregion

    #region Collection Operations

    /// <summary>
    /// Applies an action to each child element
    /// </summary>
    public static T ForEachChild<T>(this T self, Action<VisualElement> action) where T : CompositeView<T>
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        if (self.Children != null)
        {
            foreach (var child in self.Children)
            {
                action(child);
            }
        }

        return self;
    }

    /// <summary>
    /// Applies a transformation to matching children
    /// </summary>
    public static T ForEachChildOfType<T, TChild>(this T self, Action<TChild> action) 
        where T : CompositeView<T>
        where TChild : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        if (self.Children != null)
        {
            foreach (var child in self.Children)
            {
                if (child is TChild typedChild)
                    action(typedChild);
            }
        }

        return self;
    }

    /// <summary>
    /// Adds multiple children from an enumerable
    /// </summary>
    public static T WithChildren<T>(this T self, global::System.Collections.Generic.IEnumerable<VisualElement> children) where T : CompositeView<T>
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(children);

        foreach (var child in children)
        {
            if (child != null)
                self.AddChild(child);
        }

        return self;
    }

    /// <summary>
    /// Conditionally adds children based on a predicate
    /// </summary>
    public static T WithChildrenWhen<T>(this T self, bool condition, params VisualElement[] children) where T : CompositeView<T>
    {
        global::System.ArgumentNullException.ThrowIfNull(self);

        if (condition && children != null)
        {
            foreach (var child in children)
            {
                if (child != null)
                    self.AddChild(child);
            }
        }

        return self;
    }

    /// <summary>
    /// For each item in a external collection, applies an action to the element
    /// </summary>
    public static T ForEachExItems<T, TItem>(this T self, IEnumerable<TItem> items, Action<T, TItem> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(items);
        global::System.ArgumentNullException.ThrowIfNull(action);

        foreach (var item in items)
        {
            action(self, item);
        }

        return self;
    }

    /// <summary>
    /// Repeat n times with index
    /// </summary>
    public static T Repeat<T>(this T self, int times, Action<T, int> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        for (int i = 0; i < times; i++)
        {
            action(self, i);
        }

        return self;
    }

    #endregion

    #region State and Caching

    /// <summary>
    /// Executes action only once (memoized)
    /// </summary>
    public static T Once<T>(this T self, Action<T> action, ref bool executed) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        if (!executed)
        {
            action(self);
            executed = true;
        }

        return self;
    }

    /// <summary>
    /// Applies action with state that persists across calls
    /// </summary>
    public static T WithState<T, TState>(this T self, TState state, Action<T, TState> action) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        action(self, state);
        return self;
    }

    #endregion

    #region Validation and Guards

    /// <summary>
    /// Ensures a condition is met before proceeding
    /// </summary>
    public static T Guard<T>(this T self, bool condition, string? errorMessage = null) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);

        if (!condition)
            throw new global::System.InvalidOperationException(errorMessage ?? "Guard condition failed");

        return self;
    }

    /// <summary>
    /// Validates element with custom validator
    /// </summary>
    public static T Validate<T>(this T self, Func<T, bool> validator, string? errorMessage = null) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(validator);

        if (!validator(self))
            throw new global::System.InvalidOperationException(errorMessage ?? "Validation failed");

        return self;
    }

    /// <summary>
    /// Tries to apply action, catching exceptions
    /// </summary>
    public static T TryApply<T>(this T self, Action<T> action, Action<global::System.Exception>? onError = null) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(action);

        try
        {
            action(self);
        }
        catch (global::System.Exception ex)
        {
            onError?.Invoke(ex);
        }

        return self;
    }

    #endregion

    #region Composition Helpers

    /// <summary>
    /// Composes two functions into one
    /// </summary>
    public static Func<T, TResult> Compose<T, TIntermediate, TResult>(
        this Func<T, TIntermediate> first,
        Func<TIntermediate, TResult> second)
        where T : VisualElement
        where TIntermediate : VisualElement
        where TResult : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(first);
        global::System.ArgumentNullException.ThrowIfNull(second);

        return x => second(first(x));
    }

    /// <summary>
    /// Creates a fluent chain builder
    /// </summary>
    public static T Chain<T>(this T self, Action<T> configure) where T : VisualElement
    {
        global::System.ArgumentNullException.ThrowIfNull(self);
        global::System.ArgumentNullException.ThrowIfNull(configure);

        configure(self);
        return self;
    }

    #endregion
}

