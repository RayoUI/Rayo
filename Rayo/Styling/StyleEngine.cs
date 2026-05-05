using Rayo.Core;

namespace Rayo.Styling;

/// <summary>
/// Walks a <see cref="VisualElement"/> subtree and applies matching <see cref="StyleRule"/>
/// entries from a <see cref="StyleSheet"/>.
///
/// <para><b>Application order</b>:</para>
/// <list type="number">
///   <item>Normal rules sorted by <see cref="StyleRule.Specificity"/> ascending — more specific
///         rules applied last and win on conflict.</item>
///   <item>Equal specificity: declaration order wins (later = higher priority).</item>
///   <item><see cref="StyleRule.IsImportant"/> rules applied after all normal rules,
///         regardless of specificity — equivalent to CSS <c>!important</c>.</item>
///   <item>Global sheet first, component <c>BuildStyles()</c> second.</item>
/// </list>
///
/// <para><b>Performance</b>: rules are indexed by <see cref="StyleRule.TargetType"/> so each
/// element only evaluates rules that could possibly target its type.</para>
/// </summary>
public static class StyleEngine
{
    /// <summary>
    /// Applies all matching rules from <paramref name="sheet"/> to every element in the
    /// subtree rooted at <paramref name="root"/>.
    /// </summary>
    public static void Apply(StyleSheet sheet, VisualElement root,
        StyleScope scope = StyleScope.Global)
    {
        if (sheet.Count == 0) return;

        var index = BuildTypeIndex(sheet);
        Walk(root, index, scope, isRoot: true);
    }

    /// <summary>
    /// Returns a snapshot of which rules in <paramref name="sheet"/> match
    /// <paramref name="element"/> and in what order they would be applied.
    /// Useful for debugging and DevTool integration.
    /// </summary>
    public static IReadOnlyList<MatchedRule> GetComputedStyle(
        VisualElement element, StyleSheet sheet)
    {
        return SortRules(sheet)
            .Select(r => new MatchedRule(r, r.Matches(element)))
            .ToList();
    }

    // ------------------------------------------------------------------

    private static Dictionary<Type, List<StyleRule>> BuildTypeIndex(StyleSheet sheet)
    {
        var index = new Dictionary<Type, List<StyleRule>>();
        foreach (var rule in SortRules(sheet))
        {
            var key = rule.TargetType;
            if (key == null)
                continue;

            if (!index.TryGetValue(key, out var bucket))
                index[key] = bucket = new List<StyleRule>();
            bucket.Add(rule);
        }
        return index;
    }

    private static List<StyleRule> SortRules(StyleSheet sheet) =>
        sheet
            .OrderBy(r => r.IsImportant ? 1 : 0)
            .ThenBy(r => r.Specificity)
            .ToList();

    private static IEnumerable<StyleRule> GetRulesFor(
        VisualElement element, Dictionary<Type, List<StyleRule>> index)
    {
        var elementType = element.GetType();
        foreach (var rule in index.Values.SelectMany(v => v).Where(r => r.TargetType == null))
            yield return rule;

        foreach (var (key, rules) in index)
            if (key.IsAssignableFrom(elementType))
                foreach (var rule in rules)
                    yield return rule;
    }

    private static void Walk(
        VisualElement element,
        Dictionary<Type, List<StyleRule>> index,
        StyleScope scope,
        bool isRoot)
    {
        // Capture baseline on first pass; restore it before every subsequent pass so that
        // previously-applied style properties revert to their inline values before new
        // matching rules run (mirrors CSS cascade behaviour when classes change).
        element.PrepareForStyleApplication();

        var candidates = GetRulesFor(element, index)
            .OrderBy(r => r.IsImportant ? 1 : 0)
            .ThenBy(r => r.Specificity);

        foreach (var rule in candidates)
            if (rule.Matches(element))
                rule.Apply(element);

        StyleApplier.Attach(element, index.Values.SelectMany(v => v).ToList());

        foreach (var child in element.GetChildren())
        {
            if (scope == StyleScope.Local && !isRoot && child is UserControl)
                continue;
            Walk(child, index, scope, isRoot: false);
        }
    }
}

/// <summary>
/// Describes whether a single <see cref="StyleRule"/> matched a given element.
/// Returned by <see cref="StyleEngine.GetComputedStyle"/>.
/// </summary>
public readonly record struct MatchedRule(StyleRule Rule, bool IsApplied)
{
    public int  Specificity => Rule.Specificity;
    public bool IsImportant => Rule.IsImportant;
}
