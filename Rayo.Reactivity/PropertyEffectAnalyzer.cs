using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Retained for backward compatibility with existing analyzer packaging.
    /// Public writable properties without invalidation attributes now use the
    /// generator's default behavior and should not produce warnings.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PropertyEffectAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SUI001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Property missing invalidation attribute",
            messageFormat: "'{0}' in '{1}' must have [LayoutProperty], [PaintProperty] to declare its UI invalidation intent",
            category: "Rayo.Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:
                "Kept for compatibility with existing analyzer IDs. Properties without " +
                "invalidation attributes now fall back to the generator default behavior.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(Analyze, SymbolKind.Property);
        }

        private static void Analyze(SymbolAnalysisContext context)
        {
            _ = context;
        }
    }
}
