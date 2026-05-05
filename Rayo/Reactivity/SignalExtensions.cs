namespace Rayo.Reactivity;

/// <summary>
/// Helper extensions for interface-typed signals.
/// </summary>
public static class SignalExtensions
{
    /// <summary>
    /// Projects a readable signal into a computed signal.
    /// </summary>
    public static Computed<TResult> Map<T, TResult>(this IReadableSignal<T> signal, Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(selector);
        return new Computed<TResult>(() => selector(signal.Value));
    }
}
