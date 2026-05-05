using System.Collections;
using System.Runtime.CompilerServices;

namespace Rayo.Styling;

/// <summary>
/// An ordered collection of <see cref="StyleRule"/> objects.
///
/// Supports C# 12 collection-expression syntax:
/// <code>
/// StyleSheet sheet =
/// [
///     new Style&lt;Button&gt;().Height(36).BorderRadius(8),
///     new Style&lt;TextBlock&gt;().Margin(4),
/// ];
/// </code>
///
/// Rules are evaluated in order — later rules with the same target type win.
/// </summary>
[CollectionBuilder(typeof(StyleSheet), nameof(Create))]
public sealed class StyleSheet : IEnumerable<StyleRule>
{
    private readonly List<StyleRule> _rules;

    public StyleSheet()
    {
        _rules = new List<StyleRule>();
    }

    private StyleSheet(ReadOnlySpan<StyleRule> rules)
    {
        _rules = new List<StyleRule>(rules.Length);
        foreach (var rule in rules)
            _rules.Add(rule);
    }

    /// <summary>Required by <see cref="CollectionBuilderAttribute"/> for collection-expression support.</summary>
    public static StyleSheet Create(ReadOnlySpan<StyleRule> rules) => new(rules);

    /// <summary>Adds a rule to the sheet (supports object/collection initializer syntax).</summary>
    public void Add(StyleRule rule) => _rules.Add(rule);

    public int Count => _rules.Count;

    public IEnumerator<StyleRule> GetEnumerator() => _rules.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _rules.GetEnumerator();
}
