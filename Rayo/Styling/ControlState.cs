namespace Rayo.Styling;

[Flags]
public enum ControlState
{
    Normal = 0,
    Hovered = 1 << 0,
    Pressed = 1 << 1,
    Focused = 1 << 2,
    Disabled = 1 << 3,
    Selected = 1 << 4,
    Checked = 1 << 5,
    Error = 1 << 6,
}

/// <summary>Immutable state-to-value map with deterministic subset fallback.</summary>
public sealed class StateMap<T>
{
    private readonly IReadOnlyDictionary<ControlState, T> _values;

    public StateMap(T normal)
        : this(new Dictionary<ControlState, T> { [ControlState.Normal] = normal })
    {
    }

    private StateMap(IReadOnlyDictionary<ControlState, T> values)
    {
        _values = values;
    }

    public StateMap<T> With(ControlState state, T value)
    {
        var values = new Dictionary<ControlState, T>(_values) { [state] = value };
        return new StateMap<T>(values);
    }

    public T Resolve(ControlState state)
    {
        if (_values.TryGetValue(state, out var exact))
            return exact;

        var candidate = _values
            .Where(pair => pair.Key != ControlState.Normal && (state & pair.Key) == pair.Key)
            .OrderByDescending(pair => CountBits(pair.Key))
            .ThenByDescending(pair => Priority(pair.Key))
            .Select(pair => (Found: true, pair.Value))
            .FirstOrDefault();

        if (candidate.Found)
            return candidate.Value;
        return _values[ControlState.Normal];
    }

    private static int CountBits(ControlState state) =>
        System.Numerics.BitOperations.PopCount((uint)state);

    private static int Priority(ControlState state)
    {
        if (state.HasFlag(ControlState.Disabled)) return 7;
        if (state.HasFlag(ControlState.Error)) return 6;
        if (state.HasFlag(ControlState.Pressed)) return 5;
        if (state.HasFlag(ControlState.Selected) || state.HasFlag(ControlState.Checked)) return 4;
        if (state.HasFlag(ControlState.Hovered)) return 3;
        if (state.HasFlag(ControlState.Focused)) return 2;
        return 1;
    }
}
