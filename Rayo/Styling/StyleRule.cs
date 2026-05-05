using Rayo.Core;

namespace Rayo.Styling;

/// <summary>
/// Base class for a single style declaration.
///
/// <para><b>Specificity</b> (higher = wins on conflict):</para>
/// <list type="table">
///   <item><term>1</term><description>Type selector only</description></item>
///   <item><term>11</term><description>Type + one class selector</description></item>
///   <item><term>21</term><description>Type + two class selectors</description></item>
///   <item><term>101</term><description>Type + id selector</description></item>
/// </list>
///
/// Rules with equal specificity are applied in declaration order — later entries win.
/// Rules marked <see cref="IsImportant"/> are applied after all normal rules,
/// regardless of specificity.
/// </summary>
public abstract class StyleRule
{
    /// <summary>
    /// Specificity weight used to determine application order.
    /// Higher values are applied later and therefore win over lower values.
    /// </summary>
    public virtual int Specificity => 0;

    /// <summary>
    /// The primary element type this rule targets, used by <see cref="StyleEngine"/>
    /// to build a type index for O(1) rule filtering per element.
    /// Returns <c>null</c> for rules that do not restrict by type.
    /// </summary>
    public virtual Type? TargetType => null;

    /// <summary>Returns <c>true</c> if this rule should be applied to <paramref name="element"/>.</summary>
    public abstract bool Matches(VisualElement element);

    /// <summary>Applies all declared property setters to <paramref name="element"/>.</summary>
    public abstract void Apply(VisualElement element);

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one <see cref="StyleTrigger"/>-based
    /// conditional setter that requires re-evaluation when element state changes.
    /// </summary>
    public virtual bool HasStateTriggers => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one <see cref="Breakpoint"/>-based
    /// conditional setter that requires re-evaluation when the window/screen size changes.
    /// </summary>
    public virtual bool HasBreakpointConditions => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one predicate-based responsive setter
    /// that requires re-evaluation on every window size change.
    /// </summary>
    public virtual bool HasCustomBreakpoints => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one <see cref="ColorScheme"/>-based
    /// conditional setter.
    /// </summary>
    public virtual bool HasColorSchemeConditions => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one <see cref="Orientation"/>-based
    /// conditional setter.
    /// </summary>
    public virtual bool HasOrientationConditions => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one
    /// <see cref="Rayo.Core.Platform.ScreenDensity"/>-based conditional setter.
    /// </summary>
    public virtual bool HasDensityConditions => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one
    /// <see cref="Rayo.Core.Platform.PlatformType"/>-based conditional setter.
    /// </summary>
    public virtual bool HasPlatformConditions => false;

    /// <summary>
    /// Returns <c>true</c> if this rule has at least one custom min/max-width conditional
    /// setter that requires re-evaluation on every window size change.
    /// </summary>
    public virtual bool HasCustomBreakpointConditions => false;

    /// <summary>
    /// When <c>true</c> this rule is applied after all normal rules, regardless of
    /// specificity — equivalent to CSS <c>!important</c>.
    /// </summary>
    public bool IsImportant { get; protected set; }
}
