using System.ComponentModel;
using Rayo.Core;

namespace Rayo.Styling;

/// <summary>
/// Attaches a <see cref="INotifyPropertyChanged"/> listener to a <see cref="VisualElement"/>
/// so that state-conditional style rules (from <see cref="StyleRule.HasStateTriggers"/>) are
/// re-applied automatically whenever <c>IsHovered</c>, <c>IsPressed</c>, <c>IsEnabled</c>, or
/// focus changes.
///
/// Breakpoint-conditional rules are handled at the <see cref="UserControl"/> level, which
/// re-runs the full style pipeline when <see cref="BreakpointHelper.BreakpointChanged"/> fires.
///
/// Created internally by <see cref="StyleEngine"/> — not intended for direct use.
/// </summary>
internal static class StyleApplier
{
    /// <summary>
    /// Registers a <see cref="PropertyChanged"/> subscription on <paramref name="element"/>
    /// that re-applies every rule in <paramref name="rules"/> that both matches the element
    /// and has <see cref="StyleRule.HasStateTriggers"/> == <c>true</c>.
    /// </summary>
    public static void Attach(VisualElement element, IReadOnlyList<StyleRule> rules)
    {
        // Collect only the rules that care about state changes for this element
        List<StyleRule>? stateRules = null;
        foreach (var rule in rules)
        {
            if (rule.HasStateTriggers && rule.Matches(element))
            {
                stateRules ??= new List<StyleRule>();
                stateRules.Add(rule);
            }
        }

        if (stateRules == null) return;

        // Keep a snapshot so the lambda captures the built list, not the original collection
        var captured = stateRules;

        element.PropertyChanged += (sender, e) =>
        {
            if (sender is not VisualElement el) return;

            if (e.PropertyName is
                nameof(VisualElement.IsHovered) or
                nameof(VisualElement.IsPressed) or
                nameof(VisualElement.IsEnabled))
            {
                foreach (var rule in captured)
                    rule.Apply(el);
            }
        };
    }
}
