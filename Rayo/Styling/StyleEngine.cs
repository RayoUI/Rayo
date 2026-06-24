using System.Runtime.CompilerServices;
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
    private static readonly ConditionalWeakTable<StyleSheet, CompiledStyleSheet> s_compiledSheets = new();

    /// <summary>
    /// Applies all matching rules from <paramref name="sheet"/> to every element in the
    /// subtree rooted at <paramref name="root"/>.
    /// </summary>
    public static void Apply(StyleSheet sheet, VisualElement root,
        StyleScope scope = StyleScope.Global)
    {
        if (sheet.Count == 0) return;

        var compiled = GetCompiledSheet(sheet);
        Walk(root, compiled, scope, isRoot: true);
    }

    /// <summary>
    /// Applies only the rules that affect a single <paramref name="element"/>.
    /// Useful for class/state-triggered refreshes where the style system does not need
    /// to walk the entire subtree again.
    /// </summary>
    public static void ApplyToElement(StyleSheet sheet, VisualElement element)
    {
        if (sheet.Count == 0) return;

        var compiled = GetCompiledSheet(sheet);
        ApplyElementRules(element, compiled);
    }

    /// <summary>
    /// Returns a snapshot of which rules in <paramref name="sheet"/> match
    /// <paramref name="element"/> and in what order they would be applied.
    /// Useful for debugging and DevTool integration.
    /// </summary>
    public static IReadOnlyList<MatchedRule> GetComputedStyle(
        VisualElement element, StyleSheet sheet)
    {
        return GetCompiledSheet(sheet).SortedRules
            .Select(r => new MatchedRule(r, r.Matches(element)))
            .ToList();
    }

    private static CompiledStyleSheet GetCompiledSheet(StyleSheet sheet)
        => s_compiledSheets.GetValue(sheet, BuildCompiledSheet);

    private static CompiledStyleSheet BuildCompiledSheet(StyleSheet sheet)
    {
        var sortedRules = SortRules(sheet);
        var index = new Dictionary<Type, List<StyleRule>>();
        var globalRules = new List<StyleRule>();

        foreach (var rule in sortedRules)
        {
            var key = rule.TargetType;
            if (key == null)
            {
                globalRules.Add(rule);
                continue;
            }

            if (!index.TryGetValue(key, out var bucket))
                index[key] = bucket = new List<StyleRule>();
            bucket.Add(rule);
        }

        return new CompiledStyleSheet(sortedRules, globalRules, index);
    }

    private static List<StyleRule> SortRules(StyleSheet sheet) =>
        sheet
            .OrderBy(r => r.IsImportant ? 1 : 0)
            .ThenBy(r => r.Specificity)
            .ToList();

    private static List<StyleRule> GetRulesFor(
        VisualElement element, CompiledStyleSheet compiled)
    {
        var elementType = element.GetType();
        var candidates = new List<StyleRule>(compiled.GlobalRules.Count + 8);
        candidates.AddRange(compiled.GlobalRules);

        foreach (var (key, rules) in compiled.TypeIndex)
            if (key.IsAssignableFrom(elementType))
                candidates.AddRange(rules);

        return candidates;
    }

    private static void Walk(
        VisualElement element,
        CompiledStyleSheet compiled,
        StyleScope scope,
        bool isRoot)
    {
        ApplyElementRules(element, compiled);

        foreach (var child in element.GetChildren())
        {
            if (scope == StyleScope.Local && !isRoot && child is Component)
                continue;
            Walk(child, compiled, scope, isRoot: false);
        }
    }

    private static void ApplyElementRules(VisualElement element, CompiledStyleSheet compiled)
    {
        // Capture baseline on first pass; restore it before every subsequent pass so that
        // previously-applied style properties revert to their inline values before new
        // matching rules run (mirrors CSS cascade behaviour when classes change).
        element.PrepareForStyleApplication();

        var candidates = GetRulesFor(element, compiled);

        foreach (var rule in candidates)
            if (rule.Matches(element))
                rule.Apply(element);

        StyleApplier.Attach(element, candidates);
    }

    private sealed record CompiledStyleSheet(
        IReadOnlyList<StyleRule> SortedRules,
        IReadOnlyList<StyleRule> GlobalRules,
        IReadOnlyDictionary<Type, List<StyleRule>> TypeIndex);
}

/// <summary>
/// Describes whether a single <see cref="StyleRule"/> matched a given element.
/// Returned by <see cref="StyleEngine.GetComputedStyle"/>.
/// </summary>
public readonly record struct MatchedRule(StyleRule Rule, bool IsApplied)
{
    public int Specificity => Rule.Specificity;
    public bool IsImportant => Rule.IsImportant;
}
