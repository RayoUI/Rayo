namespace Rayo.Styling;

/// <summary>Pseudo-state triggers for conditional style rules, equivalent to CSS pseudo-classes.</summary>
public enum StyleTrigger
{
    /// <summary>Element is under the pointer (<c>:hover</c>).</summary>
    Hover,
    /// <summary>Element is being pressed (<c>:active</c>).</summary>
    Pressed,
    /// <summary>Element has keyboard focus (<c>:focus</c>).</summary>
    Focused,
    /// <summary>Element is disabled (<c>:disabled</c>).</summary>
    Disabled,
}
