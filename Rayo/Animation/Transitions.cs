namespace Rayo.Animation;

using Rayo.Core;
using Rayo.Controls;
using Rayo.Rendering;

/// <summary>
/// Automatic transition system for state changes
/// Inspired by CSS Transitions and MAUI Visual States
/// </summary>
public class Transition
{
    public string Property { get; set; } = "";
    public float Duration { get; set; } = 200;
    public Func<float, float> EasingFunction { get; set; } = Easing.OutQuad;

    public Transition(string property, float duration = 200, Func<float, float>? easing = null)
    {
        Property = property;
        Duration = duration;
        EasingFunction = easing ?? Easing.OutQuad;
    }
}

/// <summary>
/// Common predefined transitions
/// </summary>
public static class Transitions
{
    /// <summary>
    /// Transition for hover (fast and smooth)
    /// </summary>
    public static Transition Hover => new Transition("all", 150, Easing.OutQuad);

    /// <summary>
    /// Transition for pressed (very fast)
    /// </summary>
    public static Transition Pressed => new Transition("all", 100, Easing.OutCubic);

    /// <summary>
    /// Transition for focus (medium)
    /// </summary>
    public static Transition Focus => new Transition("all", 200, Easing.OutQuad);

    /// <summary>
    /// Transition for entering (slower)
    /// </summary>
    public static Transition Enter => new Transition("all", 300, Easing.OutBack);

    /// <summary>
    /// Transition for exiting (fast)
    /// </summary>
    public static Transition Exit => new Transition("all", 200, Easing.InQuad);

    /// <summary>
    /// General smooth transition
    /// </summary>
    public static Transition Smooth => new Transition("all", 250, Easing.InOutQuad);
}

/// <summary>
/// Helper to apply transitions to color changes on buttons
/// </summary>
public static class ButtonTransitions
{
    /// <summary>
    /// Applies hover transition to a button
    /// </summary>
    public static void ApplyHoverTransition(Button button, Color fromColor, Color toColor)
    {
        var transition = Transitions.Hover;

        var animation = new ColorAnimation(fromColor, toColor, (e, c) =>
        {
            if (e is Button btn)
            {
                // Here the color would be applied to the button
                // Requires access to the button's current color property
            }
        })
        {
            Target = button,
            Duration = transition.Duration,
            EasingFunction = transition.EasingFunction
        };

        AnimationManager.Instance.Animate(animation);
    }
}

/// <summary>
/// Visual States system (inspired by MAUI)
/// </summary>
public class VisualState
{
    public string Name { get; set; } = "";
    public Dictionary<string, object> Values { get; set; } = new();
    public Transition? Transition { get; set; }

    public VisualState(string name)
    {
        Name = name;
    }

    public VisualState SetProperty(string property, object value)
    {
        Values[property] = value;
        return this;
    }

    public VisualState WithTransition(Transition transition)
    {
        Transition = transition;
        return this;
    }
}

/// <summary>
/// Gestor de estados visuales para un elemento
/// </summary>
public class VisualStateManager
{
    private VisualElement _element;
    private Dictionary<string, VisualState> _states = new();
    private string? _currentState;

    public VisualStateManager(VisualElement element)
    {
        _element = element;
    }

    /// <summary>
    /// Defines a visual state
    /// </summary>
    public VisualStateManager DefineState(VisualState state)
    {
        _states[state.Name] = state;
        return this;
    }

    /// <summary>
    /// Changes to the specified state with transition
    /// </summary>
    public void GoToState(string stateName, bool animate = true)
    {
        if (!_states.ContainsKey(stateName) || _currentState == stateName)
            return;

        var state = _states[stateName];
        _currentState = stateName;

        if (animate && state.Transition != null)
        {
            // Apply animated transition
            // Specific implementation would depend on the properties
            // that need to be animated
        }
        else
        {
            // Apply state immediately
            // foreach (var prop in state.Values)
            // {
            //     ApplyProperty(prop.Key, prop.Value);
            // }
        }
    }

    /// <summary>
    /// Common predefined states
    /// </summary>
    public static class CommonStates
    {
        public static VisualState Normal => new VisualState("Normal");
        public static VisualState Hover => new VisualState("Hover").WithTransition(Transitions.Hover);
        public static VisualState Pressed => new VisualState("Pressed").WithTransition(Transitions.Pressed);
        public static VisualState Focused => new VisualState("Focused").WithTransition(Transitions.Focus);
        public static VisualState Disabled => new VisualState("Disabled");
    }
}
