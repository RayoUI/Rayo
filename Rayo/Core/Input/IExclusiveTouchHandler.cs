namespace Rayo.Core.Input;

/// <summary>
/// Marks a control that owns a touch gesture for its full duration.
/// Ancestor scroll containers must not take over the pointer after a drag
/// threshold has been crossed.
/// </summary>
public interface IExclusiveTouchHandler
{
}
