using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private static readonly ConditionalWeakTable<VisualElement, StateRuleSubscription> s_stateSubscriptions = new();

    /// <summary>
    /// Registers a <see cref="PropertyChanged"/> subscription on <paramref name="element"/>
    /// that re-applies every rule in <paramref name="rules"/> that both matches the element
    /// and has <see cref="StyleRule.HasStateTriggers"/> == <c>true</c>.
    /// </summary>
    public static void Attach(VisualElement element, IReadOnlyList<StyleRule> rules)
    {
        List<StyleRule>? stateRules = null;
        foreach (var rule in rules)
        {
            if (rule.HasStateTriggers && rule.Matches(element))
            {
                stateRules ??= new List<StyleRule>();
                stateRules.Add(rule);
            }
        }

        if (stateRules == null)
        {
            if (s_stateSubscriptions.TryGetValue(element, out var existing))
                existing.UpdateRules(Array.Empty<StyleRule>());
            return;
        }

        if (!s_stateSubscriptions.TryGetValue(element, out var subscription))
        {
            subscription = new StateRuleSubscription(element, stateRules);
            s_stateSubscriptions.Add(element, subscription);
            return;
        }

        subscription.UpdateRules(stateRules);
    }

    private sealed class StateRuleSubscription
    {
        private IReadOnlyList<StyleRule> _rules;

        public StateRuleSubscription(VisualElement element, IReadOnlyList<StyleRule> initialRules)
        {
            _rules = initialRules;
            element.PropertyChanged += OnPropertyChanged;
        }

        public void UpdateRules(IReadOnlyList<StyleRule> rules)
        {
            _rules = rules;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not VisualElement el)
                return;

            if (e.PropertyName is not (
                nameof(VisualElement.IsHovered) or
                nameof(VisualElement.IsPressed) or
                nameof(VisualElement.IsEnabled)))
            {
                return;
            }

            foreach (var rule in _rules)
                if (rule.Matches(el))
                    rule.Apply(el);
        }
    }
}
