#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Rayo.Reactivity
{
    [Generator]
    public class FluentApiGenerator : IIncrementalGenerator
    {
        private const string GeneratorVersion = "6.0.0"; // Merged ReactiveExtensions + StyleExtensions into one Extensions class

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all public classes that inherit from VisualElement (directly or via VisualElement<T>)
            // and don't have [NotFluent] attribute
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidateClass(node),
                    transform: static (ctx, _) => GetClassInfo(ctx));

            // Read MSBuild properties exposed by the consuming project via CompilerVisibleProperty.
            //
            //   <CompilerVisibleProperty Include="SourceGeneratorNamespace" />
            //   <CompilerVisibleProperty Include="RootNamespace" />
            //
            // SourceGeneratorNamespace supports macros (see ResolveMacros):
            //   {class}    → namespace of the annotated class
            //   {root}     → first dotted segment of the class namespace
            //   {assembly} → project RootNamespace MSBuild property
            //   {null}     → global namespace (no namespace wrapper)
            //
            // Example: <SourceGeneratorNamespace>{root}.Extensions</SourceGeneratorNamespace>
            var generatorOptions = context.AnalyzerConfigOptionsProvider
                .Select(static (opts, _) =>
                {
                    opts.GlobalOptions.TryGetValue("build_property.SourceGeneratorNamespace", out var ns);
                    opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNs);
                    var template = string.IsNullOrWhiteSpace(ns) ? null : ns!.Trim();
                    var assemblyNs = string.IsNullOrWhiteSpace(rootNs) ? null : rootNs!.Trim();
                    return (Template: template, AssemblyNamespace: assemblyNs);
                });

            // Combine compilation, classes, and the generator options
            var compilationAndClasses = context.CompilationProvider
                .Combine(classDeclarations.Collect())
                .Combine(generatorOptions);

            // Generate source for each class
            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => ExecuteGeneration(
                    source.Left.Left,
                    source.Left.Right,
                    spc,
                    source.Right.Template,
                    source.Right.AssemblyNamespace));
        }

        private static bool IsCandidateClass(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax classDeclaration)
                return false;

            // Check if class has a base type (required for inheritance check)
            // This is a fast syntactic check to filter out classes with no base type at all
            if (classDeclaration.BaseList == null || classDeclaration.BaseList.Types.Count == 0)
                return false;

            // Accept any class with a base type - we'll do full semantic check in GetClassInfo.
            // This is more permissive than checking for "VisualElement" in the syntax,
            // because classes might inherit through intermediate types like ContentView<T>, View<T>, etc.
            return true;
        }

        private static ClassDeclarationSyntax? GetClassInfo(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var model = context.SemanticModel;
            var classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (classSymbol == null)
                return null;

            // Skip non-public types - extension methods can't access them
            if (classSymbol.DeclaredAccessibility != Accessibility.Public)
                return null;

            // Check for [NotFluent] attribute - skip this class entirely if present
            var hasNotFluent = classSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name.Contains("NotFluent") == true);

            if (hasNotFluent)
                return null;

            // Check if class inherits from VisualElement (direct or via VisualElement<T>)
            if (InheritsFromVisualElement(classSymbol))
                return classDeclaration;

            return null;
        }

        /// <summary>
        /// Checks if a type inherits from VisualElement (directly or via VisualElement&lt;T&gt;).
        /// </summary>
        private static bool InheritsFromVisualElement(INamedTypeSymbol classSymbol)
        {
            var currentType = classSymbol.BaseType;

            while (currentType != null)
            {
                // Check for exact match: Rayo.Core.VisualElement (non-generic or generic)
                // Handle both "VisualElement" and "VisualElement<T>"
                if (currentType.ContainingNamespace?.ToDisplayString() == "Rayo.Core")
                {
                    // Check the unbound generic type name (e.g., "VisualElement" for both "VisualElement" and "VisualElement<T>")
                    var baseName = currentType.IsGenericType 
                        ? currentType.OriginalDefinition.Name 
                        : currentType.Name;

                    if (baseName == "VisualElement")
                    {
                        return true;
                    }
                }

                // Move to base type
                currentType = currentType.BaseType;

                // Stop at System.Object
                if (currentType?.SpecialType == SpecialType.System_Object)
                    break;
            }

            return false;
        }

        /// <summary>
        /// Gets all properties from the class and its base classes (entire inheritance hierarchy).
        /// Extension types don't support polymorphism, so each class needs its own complete set of methods.
        /// </summary>
        private static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol classSymbol)
        {
            var properties = new List<IPropertySymbol>();
            var currentType = classSymbol;
            var processedNames = new HashSet<string>();

            // Walk up the inheritance hierarchy
            while (currentType != null)
            {
                // Get properties declared on this type
                var declaredProperties = currentType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .ToList();

                foreach (var prop in declaredProperties)
                {
                    // Skip if we already processed a property with this name (property hiding/new keyword)
                    // Keep the most derived version
                    if (!processedNames.Contains(prop.Name))
                    {
                        properties.Add(prop);
                        processedNames.Add(prop.Name);
                    }
                }

                // Move to base type
                currentType = currentType.BaseType;

                // Stop at System.Object or if we reach a type outside of user code
                if (currentType?.SpecialType == SpecialType.System_Object)
                    break;
            }

            return properties;
        }

        private static void ExecuteGeneration(
            Compilation compilation,
            ImmutableArray<ClassDeclarationSyntax?> classes,
            SourceProductionContext context,
            string? namespaceTemplate,
            string? assemblyNamespace)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            foreach (var candidateClass in classes)
            {
                // Filter out null entries from GetClassInfo
                if (candidateClass == null)
                    continue;

                var model = compilation.GetSemanticModel(candidateClass.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(candidateClass, context.CancellationToken) as INamedTypeSymbol;

                if (classSymbol == null)
                    continue;

                // Collect all public properties from the entire inheritance hierarchy.
                // Each class generates methods for its own properties AND all inherited ones,
                // ensuring fluent chains always return the most-derived type.
                var properties = GetAllProperties(classSymbol)
                    .Where(p =>
                        p.DeclaredAccessibility == Accessibility.Public &&
                        !p.IsStatic &&
                        p.GetMethod != null &&
                        // Skip properties with non-public setters or no setter (unless read-only collection or List)
                        (p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public || 
                         IsReadOnlyCollectionProperty(p) || 
                         IsListProperty(p, out _)) &&
                        // Skip obsolete properties
                        !p.GetAttributes().Any(a => a.AttributeClass?.Name == "ObsoleteAttribute"))
                    .ToList();

                // Filter properties based on [NotFluent] attribute
                var reactiveProperties = new List<IPropertySymbol>();
                foreach (var prop in properties)
                {
                    // Check for [NotFluent] - skip this property if present
                    var hasNotFluent = prop.GetAttributes()
                        .Any(a => a.AttributeClass?.Name.Contains("NotFluent") == true);

                    if (hasNotFluent)
                        continue;

                    // Include all properties that don't have [NotFluent]
                    reactiveProperties.Add(prop);
                }

                if (reactiveProperties.Count == 0)
                    continue;

                // Resolve per-class namespace: expand macros in the template, then pass the
                // concrete namespace string (or null = global) to the source generator.
                var resolvedNamespace = ResolveMacros(namespaceTemplate, classSymbol, assemblyNamespace);

                // Generate extension type with Set and SetReactive methods
                var source = GenerateExtensionSource(classSymbol, reactiveProperties, resolvedNamespace);

                // Use fully-qualified name to avoid collisions between:
                //   • same-named classes in different namespaces (e.g. Controls.Shape vs Controls.Shapes.Shape<T>)
                //   • generic and non-generic variants with the same name in the same namespace
                //     (e.g. VisualElement  vs  VisualElement<T>  →  add "_T" suffix for generic variant)
                //   • nested classes with the same name in different parent classes (include declaring type)

                // Build the full qualified hint starting with namespace
                var qualifiedHint = classSymbol.ContainingNamespace.IsGlobalNamespace
                    ? ""
                    : classSymbol.ContainingNamespace.ToDisplayString().Replace(".", "_") + "_";

                // If this is a nested class, include the declaring type hierarchy
                var typeHierarchy = new List<string>();
                var currentType = classSymbol;
                while (currentType != null)
                {
                    var typeName = currentType.Name;
                    // Add generic type parameter names for generic types
                    if (currentType.IsGenericType)
                    {
                        var typeParamSuffix = string.Join("_", currentType.TypeParameters.Select(tp => tp.Name));
                        typeName += $"_{typeParamSuffix}";
                    }
                    typeHierarchy.Insert(0, typeName);
                    currentType = currentType.ContainingType; // Move to parent type (null for non-nested classes)
                }

                qualifiedHint += string.Join("_", typeHierarchy);
                var fileName = $"{qualifiedHint}.ReactiveExtension.g.cs";

                context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
            }
        }

        /// <summary>
        /// Expands macros in a <c>SourceGeneratorNamespace</c> template for a specific class.
        /// </summary>
        /// <remarks>
        /// Supported macros (case-insensitive, curly-brace delimited):
        /// <list type="table">
        ///   <item><term>{class}</term>    <description>Full namespace of the annotated class (e.g. <c>MyApp.Controls</c>). Falls back to the class name when the class is in the global namespace.</description></item>
        ///   <item><term>{root}</term>     <description>First dotted segment of the class namespace (e.g. <c>MyApp</c> from <c>MyApp.Controls.Widgets</c>). Falls back to the class name when the class has no namespace.</description></item>
        ///   <item><term>{assembly}</term> <description>The project's <c>RootNamespace</c> MSBuild property. Falls back to <c>{root}</c> when the property is not set.</description></item>
        ///   <item><term>{null}</term>     <description>Signals that no namespace wrapper should be emitted (global namespace). When the entire resolved string equals <c>{null}</c> the method returns <c>null</c>.</description></item>
        /// </list>
        /// Macros may be freely mixed with literal text, e.g. <c>{root}.Extensions</c>.
        /// When <paramref name="template"/> is <c>null</c> or empty the method returns <c>null</c>,
        /// preserving the original behaviour (use the class's own namespace).
        /// </remarks>
        private static string? ResolveMacros(string? template, INamedTypeSymbol classSymbol, string? assemblyNamespace)
        {
            if (string.IsNullOrWhiteSpace(template))
                return null;

            var classNs = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();

            // {root} = first dotted segment of the class namespace; falls back to class name
            var rootNs = classNs != null
                ? classNs.Split('.')[0]
                : classSymbol.Name;

            // {assembly} = RootNamespace MSBuild property; falls back to {root}
            var asmNs = assemblyNamespace ?? rootNs;

            // {class} = full class namespace; falls back to class name
            var classNsFallback = classNs ?? classSymbol.Name;

            // Case-insensitive replace helper (netstandard2.0 lacks the StringComparison overload)
            static string ReplaceCI(string src, string oldVal, string newVal)
            {
                var sb2 = new StringBuilder();
                int start = 0, idx;
                while ((idx = src.IndexOf(oldVal, start, System.StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    sb2.Append(src, start, idx - start);
                    sb2.Append(newVal);
                    start = idx + oldVal.Length;
                }
                sb2.Append(src, start, src.Length - start);
                return sb2.ToString();
            }

            var resolved = ReplaceCI(ReplaceCI(ReplaceCI(template!, "{class}", classNsFallback), "{root}", rootNs), "{assembly}", asmNs);

            // {null} signals the global namespace — return null so no namespace block is emitted.
            // Support bare "{null}" as the whole value, or as a token inside a larger expression.
            if (string.Equals(resolved.Trim(), "{null}", System.StringComparison.OrdinalIgnoreCase))
                return null;

            // Strip any remaining {null} tokens that appear inside a larger expression
            // (unusual but handled gracefully — remove the token and clean up stray dots).
            resolved = ReplaceCI(resolved, "{null}", "").Trim('.');

            // Collapse consecutive dots that may result from removed tokens (e.g. "Foo..Bar")
            while (resolved.Contains(".."))
                resolved = resolved.Replace("..", ".");

            return string.IsNullOrWhiteSpace(resolved) ? null : resolved.Trim();
        }

        /// <summary>
        /// Determines if a property change requires layout recalculation or just repaint.
        /// Reads [LayoutProperty] or [PaintProperty] attribute.
        /// Defaults to MarkNeedsPaint() when no attribute is present.
        /// </summary>
        private static string GetMarkMethod(IPropertySymbol property)
        {
            foreach (var attr in property.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName == "LayoutPropertyAttribute" || attrName == "LayoutProperty")
                    return "MarkNeedsLayout()";
                if (attrName == "PaintPropertyAttribute" || attrName == "PaintProperty")
                    return "MarkNeedsPaint()";
            }
            // No attribute — use the generator default behavior.
            return "MarkNeedsPaint()";
        }

        /// <summary>
        /// Checks if a property is a read-only collection property (like Layout.Children).
        /// These need special handling - generate methods that use Add/Clear instead of assignment.
        /// </summary>
        private static bool IsReadOnlyCollectionProperty(IPropertySymbol property)
        {
            // Must have getter but no setter
            if (property.SetMethod != null || property.GetMethod == null)
                return false;

            // Check if the type is a collection type
            var type = property.Type;
            if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
                return false;

            var typeDefinition = namedType.OriginalDefinition.ToDisplayString();

            // Check for read-only collection types
            var readOnlyCollectionTypes = new[]
            {
                "System.Collections.Generic.IReadOnlyList<T>",
                "System.Collections.Generic.IReadOnlyCollection<T>",
                "System.Collections.Generic.IEnumerable<T>"
            };

            return readOnlyCollectionTypes.Contains(typeDefinition);
        }

        /// <summary>
        /// Checks if a property is a List<T> property.
        /// These generate SetXxx(params T[] items) methods that create a new list.
        /// </summary>
        private static bool IsListProperty(IPropertySymbol property, out ITypeSymbol? elementType)
        {
            elementType = null;

            // Must have both getter and setter
            if (property.SetMethod == null || property.GetMethod == null)
                return false;

            // Check if the type is List<T>
            var type = property.Type;
            if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
                return false;

            var typeDefinition = namedType.OriginalDefinition.ToDisplayString();

            if (typeDefinition == "System.Collections.Generic.List<T>")
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects if a type is a collection type (IList, List, ICollection, IEnumerable, etc.)
        /// </summary>
        private static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
        {
            elementType = null;

            // Check if it's a generic type
            if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
                return false;

            var typeDefinition = namedType.OriginalDefinition.ToDisplayString();

            // Check for common collection interfaces and classes
            var collectionTypes = new[]
            {
                "System.Collections.Generic.IList<T>",
                "System.Collections.Generic.List<T>",
                "System.Collections.Generic.ICollection<T>",
                "System.Collections.Generic.IEnumerable<T>",
                "System.Collections.Generic.IReadOnlyList<T>",
                "System.Collections.Generic.IReadOnlyCollection<T>"
            };

            if (collectionTypes.Contains(typeDefinition))
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects whether a generic class follows the CRTP (Curiously Recurring Template Pattern):
        ///   class Foo&lt;T&gt; where T : Foo&lt;T&gt;
        /// Returns the type parameter that is constrained to the class itself.
        /// </summary>
        private static bool IsCRTPGenericClass(INamedTypeSymbol classSymbol, out ITypeParameterSymbol? crtpTypeParam)
        {
            crtpTypeParam = null;
            if (!classSymbol.IsGenericType) return false;

            foreach (var typeParam in classSymbol.TypeParameters)
            {
                foreach (var constraint in typeParam.ConstraintTypes)
                {
                    if (constraint is INamedTypeSymbol constraintType &&
                        SymbolEqualityComparer.Default.Equals(
                            constraintType.OriginalDefinition, classSymbol.OriginalDefinition))
                    {
                        crtpTypeParam = typeParam;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the most appropriate collection interface type for reactive binding
        /// (IEnumerable for maximum flexibility)
        /// </summary>
        private static string GetCollectionInterfaceType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var displayFormat = new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | 
                                         SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
                var elementType = namedType.TypeArguments[0].ToDisplayString(displayFormat);
                return $"global::System.Collections.Generic.IEnumerable<{elementType}>";
            }
            
            var fullDisplayFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | 
                                     SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            return type.ToDisplayString(fullDisplayFormat);
        }

        private static string GenerateExtensionSource(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, string? customNamespace = null)
        {
            // customNamespace is the already-resolved namespace string (macros already expanded by
            // ResolveMacros). When non-null the extension class is placed in that namespace instead
            // of the annotated class's own namespace. null means global namespace.
            // All type references inside methods still use fully-qualified (global::…) names.
            var classNamespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();
            var namespaceName = customNamespace ?? classNamespaceName;
            var isGlobalNamespace = namespaceName == null;

            var className = classSymbol.Name;

            // For generic classes, append type-param names so the generated static class names are unique.
            // e.g. VisualElement<T>  →  extensionClassName = "VisualElementT"
            //      VisualElement     →  extensionClassName = "VisualElement"
            var extensionClassName = classSymbol.IsGenericType
                ? className + string.Join("", classSymbol.TypeParameters.Select(tp => tp.Name))
                : className;

            // Determine the accessibility of the target class
            var accessibility = classSymbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                _ => "internal" // Default to internal for safety
            };

            // Detect and preserve generic type parameters
            var genericParams = "";
            var genericConstraints = new List<string>();
            var extensionGenericParams = "";
            if (classSymbol.IsGenericType)
            {
                var typeParams = string.Join(", ", classSymbol.TypeParameters.Select(tp => tp.Name));
                genericParams = $"<{typeParams}>";
                extensionGenericParams = $"<{typeParams}>";

                // Capture constraints for generic parameters
                foreach (var typeParam in classSymbol.TypeParameters)
                {
                    var constraints = new List<string>();
                    
                    if (typeParam.HasReferenceTypeConstraint)
                        constraints.Add("class");
                    if (typeParam.HasValueTypeConstraint)
                        constraints.Add("struct");
                    if (typeParam.HasUnmanagedTypeConstraint)
                        constraints.Add("unmanaged");
                    if (typeParam.HasConstructorConstraint)
                        constraints.Add("new()");
                    
                    foreach (var constraintType in typeParam.ConstraintTypes)
                    {
                        constraints.Add(constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    }
                    
                    if (constraints.Count > 0)
                    {
                        genericConstraints.Add($"            where {typeParam.Name} : {string.Join(", ", constraints)}");
                    }
                }
            }

            var fullClassName = $"{className}{genericParams}";
            var fullyQualifiedClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // --- CRTP detection ---
            // e.g. abstract class Shape<T> where T : Shape<T>
            bool isCRTP = IsCRTPGenericClass(classSymbol, out var crtpTypeParam);

            // All paths use classic static extension methods (no C# 14 extension blocks).
            // Return type: "T" for CRTP (preserves derived type), concrete class name otherwise.
            string methodReturnType = isCRTP ? crtpTypeParam!.Name : fullyQualifiedClassName;

            // Return expression inside generated method bodies.
            string returnExpression = isCRTP ? $"({crtpTypeParam!.Name})(object)self" : "self";

            // For CRTP classes, the CRTP type parameter must appear on every method (classic style).
            // extensionGenericParams was already set above for generic classes; for CRTP it was
            // intentionally left as the full "<T>" set, which is correct here.

            // Generate signature hash including generator version and all property details
            var propertySignature = string.Join("|", properties.Select(p =>
                $"{p.Name}:{p.Type.ToDisplayString()}"));
            var signatureHash = $"{GeneratorVersion}|{propertySignature}".GetHashCode();

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine($"// Generator: ReactivePropertyGenerator v{GeneratorVersion}");
            sb.AppendLine("// This file was automatically generated. Do not edit directly.");
            sb.AppendLine($"// Signature Hash: {signatureHash:X8}");
            sb.AppendLine($"// Properties: {properties.Count}");
            if (customNamespace != null)
                sb.AppendLine($"// Namespace override (SourceGeneratorNamespace): {customNamespace}");
            sb.AppendLine("// " + new string('-', 70));
            foreach (var prop in properties)
            {
                var markMethod = GetMarkMethod(prop);
                sb.AppendLine($"//   {prop.Name} ({prop.Type.Name}) -> {markMethod}");
            }
            sb.AppendLine("// " + new string('-', 70));
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using Rayo.Reactivity;");
            sb.AppendLine();

            // Only generate namespace declaration if not in global namespace
            if (!isGlobalNamespace)
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            // Generate static extension class for the target class
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Fluent extensions for {className}: signal-aware fluent setters, IReadableSignal&lt;T&gt; bindings,");
            sb.AppendLine($"    /// and Style&lt;T&gt; property setters. Generated by ReactivePropertyGenerator.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    {accessibility} static class {extensionClassName}Extensions");
            sb.AppendLine("    {");

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                // Use a format that includes nullable annotations
                var displayFormat = new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | 
                                         SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
                var propertyType = property.Type.ToDisplayString(displayFormat);
                var propertyTypeFullName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isBrushProperty = propertyTypeFullName == "global::Rayo.Rendering.Brushes.Brush" ||
                    propertyTypeFullName == "global::Rayo.Rendering.Brushes.Brush?";
                var setMethodName = $"{propertyName}";
                var paramName = char.ToLower(propertyName[0]) + propertyName.Substring(1);
                var markMethod = GetMarkMethod(property);

                // Detect if property is nullable
                var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated;
                
                // Detect if property is a collection type that needs special handling
                var isCollectionProperty = IsCollectionType(property.Type, out var elementType);
                var collectionInterfaceType = isCollectionProperty ? GetCollectionInterfaceType(property.Type) : null;

                // Check if this is a read-only collection property (like Layout.Children)
                var isReadOnlyCollection = IsReadOnlyCollectionProperty(property);

                // Check if this is a List<T> property
                var isListProperty = IsListProperty(property, out var listElementType);

                // Skip generating an extension method if the class already has an instance method
                // with the same name — the manual method shadows the property, making
                // `self.PropertyName = ...` a CS1656 error inside the extension block.
                bool classHasMethodWithSameName = classSymbol.GetMembers(propertyName)
                    .OfType<IMethodSymbol>()
                    .Any(m => !m.IsStatic);

                if (classHasMethodWithSameName && (isListProperty || isReadOnlyCollection || isCollectionProperty))
                    continue;

                // NOTE: We generate Children for each class in the hierarchy because
                // C# extension methods don't automatically apply to derived types.
                // So if Layout has Children, HStack needs its own Children too.

                if (isListProperty)
                {
                    // Skip if this is an inherited List property from a VisualElement base class
                    // to avoid duplicate Children methods
                    if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, classSymbol))
                    {
                        var declaringTypeInheritsVisualElement = InheritsFromVisualElement(property.ContainingType);

                        if (declaringTypeInheritsVisualElement)
                        {
                            // Skip - base class already generates this
                            continue;
                        }
                    }

                    // For List<T> properties, generate SetXxx(params T[] items) that creates a new list
                    var elementDisplayFormat = new SymbolDisplayFormat(
                        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                             SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
                    var elementTypeString = listElementType!.ToDisplayString(elementDisplayFormat);

                    var nonNullableElementTypeSymbol = listElementType;
                    var hasNullableReferenceElement = listElementType.IsReferenceType &&
                        listElementType.NullableAnnotation == NullableAnnotation.Annotated;

                    if (hasNullableReferenceElement)
                    {
                        nonNullableElementTypeSymbol = listElementType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                    }

                    var nonNullableElementTypeString = nonNullableElementTypeSymbol.ToDisplayString(elementDisplayFormat);

                    // Classic static extension method
                    var methodPrefix = $"public static {methodReturnType}";
                    var thisParam = $"this {fullyQualifiedClassName} self, ";
                    var genericConstraintsString = "";
                    if (classSymbol.IsGenericType && genericConstraints.Count > 0)
                    {
                        genericConstraintsString = "\n" + string.Join("\n", genericConstraints);
                    }

                    // For reference types, only generate nullable versions to avoid ambiguity
                    // For value types, generate non-nullable version
                    if (hasNullableReferenceElement)
                    {
                        // Generate only nullable versions for reference types
                        sb.AppendLine();
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} by creating a new list from the provided items.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}params {elementTypeString}[] items){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{propertyName} = new global::System.Collections.Generic.List<{elementTypeString}>(items.Where(i => i != null)!);");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();

                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} by creating a new list from the provided enumerable.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::System.Collections.Generic.IEnumerable<{elementTypeString}> items){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{propertyName} = new global::System.Collections.Generic.List<{elementTypeString}>(items.Where(i => i != null)!);");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();
                    }
                    else
                    {
                        // Generate non-nullable versions for value types or non-nullable reference types
                        sb.AppendLine();
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} by creating a new list from the provided items.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}params {elementTypeString}[] items){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{propertyName} = new global::System.Collections.Generic.List<{elementTypeString}>(items);");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();

                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} by creating a new list from the provided enumerable.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::System.Collections.Generic.IEnumerable<{elementTypeString}> items){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{propertyName} = new global::System.Collections.Generic.List<{elementTypeString}>(items);");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();
                    }

                    // Generate signal overload for List properties
                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Signal overload for {setMethodName}.");
                    sb.AppendLine($"            /// Subscribes to changes and updates the property automatically.");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.IReadableSignal<global::System.Collections.Generic.List<{elementTypeString}>> {paramName}Signal){genericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                if ({paramName}Signal == null)");
                    sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}Signal));");
                    sb.AppendLine();
                    sb.AppendLine($"                var subscription = {paramName}Signal.Subscribe(new{propertyName} =>");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        self.{propertyName} = new{propertyName};");
                    sb.AppendLine("                    });");
                    sb.AppendLine("                });");
                    sb.AppendLine();
                    sb.AppendLine("                self.RegisterDisposable(subscription);");
                    sb.AppendLine($"                self.{propertyName} = {paramName}Signal.Value;");
                    sb.AppendLine();
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");
                    sb.AppendLine();

                    // Also generate reactive overload for IEnumerable (similar to SetItems pattern)
                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Signal overload for {setMethodName} with IEnumerable.");
                    sb.AppendLine($"            /// Subscribes to changes and converts to List automatically.");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.IReadableSignal<global::System.Collections.Generic.IEnumerable<{elementTypeString}>> {paramName}Signal){genericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                if ({paramName}Signal == null)");
                    sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}Signal));");
                    sb.AppendLine();
                    sb.AppendLine($"                var subscription = {paramName}Signal.Subscribe(new{propertyName} =>");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        self.{propertyName} = new global::System.Collections.Generic.List<{elementTypeString}>(new{propertyName} ?? global::System.Linq.Enumerable.Empty<{elementTypeString}>());");
                    sb.AppendLine("                    });");
                    sb.AppendLine("                });");
                    sb.AppendLine();
                    sb.AppendLine("                self.RegisterDisposable(subscription);");
                    sb.AppendLine($"                self.{propertyName} = new global::System.Collections.Generic.List<{elementTypeString}>({paramName}Signal.Value ?? global::System.Linq.Enumerable.Empty<{elementTypeString}>());");
                    sb.AppendLine();
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");
                    sb.AppendLine();
                }
                else if (isReadOnlyCollection)
                {
                    // Classic static extension method
                    var methodPrefix = $"public static {methodReturnType}";
                    var thisParam = $"this {fullyQualifiedClassName} self, ";
                    var genericConstraintsString = "";
                    if (classSymbol.IsGenericType && genericConstraints.Count > 0)
                    {
                        genericConstraintsString = "\n" + string.Join("\n", genericConstraints);
                    }

                    // For read-only collections, generate SetChildren that uses Clear() and Add()
                    sb.AppendLine();
                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Sets the {propertyName} by clearing and adding new items.");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}params global::Rayo.Core.VisualElement[] items){genericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                self.Clear();");
                    sb.AppendLine($"                foreach (var item in items)");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    self.Add(item);");
                    sb.AppendLine($"                }}");
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");
                    sb.AppendLine();

                    // Also generate IEnumerable overload
                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Sets the {propertyName} by clearing and adding new items from enumerable.");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::System.Collections.Generic.IEnumerable<global::Rayo.Core.VisualElement> items){genericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                self.Clear();");
                    sb.AppendLine($"                foreach (var item in items)");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    self.Add(item);");
                    sb.AppendLine($"                }}");
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");
                    sb.AppendLine();
                }
                else
                {
                    // Classic static extension method
                    var methodPrefix = $"public static {methodReturnType}";
                    var thisParam = $"this {fullyQualifiedClassName} self, ";
                    var genericConstraintsString = "";
                    if (classSymbol.IsGenericType && genericConstraints.Count > 0)
                    {
                        genericConstraintsString = "\n" + string.Join("\n", genericConstraints);
                    }

                    // Generate Set method (non-reactive) for regular properties
                    sb.AppendLine();
                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Sets the {propertyName} property.");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}{propertyType} {paramName}){genericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                self.{propertyName} = {paramName};");
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");
                    sb.AppendLine();

                    if (isBrushProperty)
                    {
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} property.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Rendering.Color {paramName}){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{propertyName} = {paramName};");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();
                    }
                }

                // Generate signal overload only for regular properties (not read-only collections or list properties)
                if (!isReadOnlyCollection && !isListProperty)
                {
                    // Classic static extension method
                    var methodPrefix = $"public static {methodReturnType}";
                    var thisParam = $"this {fullyQualifiedClassName} self, ";
                    var genericConstraintsString = "";
                    if (classSymbol.IsGenericType && genericConstraints.Count > 0)
                    {
                        genericConstraintsString = "\n" + string.Join("\n", genericConstraints);
                    }

                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// Signal overload for {setMethodName}.");
                    sb.AppendLine($"            /// Subscribes to changes and updates the property automatically.");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.IReadableSignal<{propertyType}> {paramName}Signal){genericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                if ({paramName}Signal == null)");
                    sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}Signal));");
                    sb.AppendLine();
                    sb.AppendLine($"                var subscription = {paramName}Signal.Subscribe(new{propertyName} =>");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        self.{propertyName} = new{propertyName};");
                    sb.AppendLine("                    });");
                    sb.AppendLine("                });");
                    sb.AppendLine();
                    sb.AppendLine("                self.RegisterDisposable(subscription);");
                    sb.AppendLine($"                self.{propertyName} = {paramName}Signal.Value;");
                    sb.AppendLine();
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");

                    if (isBrushProperty)
                    {
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Signal overload for {setMethodName}.");
                        sb.AppendLine($"            /// Subscribes to changes and updates the property automatically.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.IReadableSignal<global::Rayo.Rendering.Color> {paramName}Signal){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                if ({paramName}Signal == null)");
                        sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}Signal));");
                        sb.AppendLine();
                        sb.AppendLine($"                var subscription = {paramName}Signal.Subscribe(new{propertyName} =>");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                        sb.AppendLine("                    {");
                        sb.AppendLine($"                        self.{propertyName} = new{propertyName};");
                        sb.AppendLine("                    });");
                        sb.AppendLine("                });");
                        sb.AppendLine();
                        sb.AppendLine("                self.RegisterDisposable(subscription);");
                        sb.AppendLine($"                self.{propertyName} = {paramName}Signal.Value;");
                        sb.AppendLine();
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                    }
                }


                // For collection properties (but NOT read-only collections or List properties), generate additional overloads for IEnumerable conversion
                if (isCollectionProperty && collectionInterfaceType != null && !isReadOnlyCollection && !isListProperty)
                {
                    var propertyTypeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var collectionElementTypeString = (property.Type as INamedTypeSymbol)?.TypeArguments[0]
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";

                    // Check if property type is IList or List (needs conversion from IEnumerable)
                    var needsConversion = propertyTypeName.Contains("IList<") || propertyTypeName.Contains("List<");

                    if (needsConversion)
                    {
                        // Classic static extension method
                        var methodPrefix = $"public static {fullyQualifiedClassName}";
                        var thisParam = $"this {fullyQualifiedClassName} self, ";
                        var genericConstraintsString = "";
                        if (classSymbol.IsGenericType && genericConstraints.Count > 0)
                        {
                            genericConstraintsString = "\n" + string.Join("\n", genericConstraints);
                        }

                        // Generate overload: SetItems(IEnumerable<T>) -> converts to List<T>
                        sb.AppendLine();
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} property from an IEnumerable.");
                        sb.AppendLine($"            /// Automatically converts to {property.Type.Name}.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}{collectionInterfaceType} {paramName}){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{propertyName} = {paramName}?.ToList() ?? new global::System.Collections.Generic.List<{collectionElementTypeString}>();");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();

                        // Generate signal overload: SetItems(IReadableSignal<IEnumerable<T>>)
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Signal overload for {setMethodName} with IEnumerable.");
                        sb.AppendLine($"            /// Subscribes to changes and converts to {property.Type.Name} automatically.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.IReadableSignal<{collectionInterfaceType}> {paramName}Signal){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                if ({paramName}Signal == null)");
                        sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}Signal));");
                        sb.AppendLine();
                        sb.AppendLine($"                var subscription = {paramName}Signal.Subscribe(new{propertyName} =>");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                        sb.AppendLine("                    {");
                        sb.AppendLine($"                        self.{propertyName} = new{propertyName}?.ToList() ?? new global::System.Collections.Generic.List<{collectionElementTypeString}>();");
                        sb.AppendLine("                    });");
                        sb.AppendLine("                });");
                        sb.AppendLine();
                        sb.AppendLine("                self.RegisterDisposable(subscription);");
                        sb.AppendLine($"                self.{propertyName} = {paramName}Signal.Value?.ToList() ?? new global::System.Collections.Generic.List<{collectionElementTypeString}>();");
                        sb.AppendLine();
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();

                        // Also generate reactive overload for List<T> specifically since IReadableSignal<T> is not covariant
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Signal overload for {setMethodName} with List.");
                        sb.AppendLine($"            /// Subscribes to changes and converts to {property.Type.Name} automatically.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.IReadableSignal<global::System.Collections.Generic.List<{elementType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>> {paramName}Signal){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                if ({paramName}Signal == null)");
                        sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}Signal));");
                        sb.AppendLine();
                        sb.AppendLine($"                var subscription = {paramName}Signal.Subscribe(new{propertyName} =>");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                        sb.AppendLine("                    {");
                        sb.AppendLine($"                        self.{propertyName} = new{propertyName};");
                        sb.AppendLine("                    });");
                        sb.AppendLine("                });");
                        sb.AppendLine();
                        sb.AppendLine("                self.RegisterDisposable(subscription);");
                        sb.AppendLine($"                self.{propertyName} = {paramName}Signal.Value?.ToList() ?? new global::System.Collections.Generic.List<{collectionElementTypeString}>();");
                        sb.AppendLine();
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                        sb.AppendLine();

                        // Generate SignalList overload that subscribes to changes
                        sb.AppendLine($"            /// <summary>");
                        sb.AppendLine($"            /// Sets the {propertyName} property from a SignalList and subscribes to changes.");
                        sb.AppendLine($"            /// Automatically updates when items are added/removed/modified.");
                        sb.AppendLine($"            /// </summary>");
                        sb.AppendLine($"            {methodPrefix} {setMethodName}{extensionGenericParams}({thisParam}global::Rayo.Reactivity.SignalList<{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {paramName}){genericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                if ({paramName} == null)");
                        sb.AppendLine($"                    throw new global::System.ArgumentNullException(nameof({paramName}));");
                        sb.AppendLine();
                        sb.AppendLine($"                // Set the property to the SignalList");
                        sb.AppendLine($"                self.{propertyName} = {paramName}.ToList();");
                        sb.AppendLine();
                        sb.AppendLine($"                // Subscribe to changes");
                        sb.AppendLine($"                var subscription = {paramName}.Subscribe(() =>");
                        sb.AppendLine("                {");
                        sb.AppendLine($"                    global::Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(self, () =>");
                        sb.AppendLine("                    {");
                        sb.AppendLine($"                        // Trigger property setter to ensure UI updates");
                        sb.AppendLine($"                        self.{propertyName} = {paramName}.ToList();");
                        sb.AppendLine("                    });");
                        sb.AppendLine("                });");
                        sb.AppendLine();
                        sb.AppendLine("                self.RegisterDisposable(subscription);");
                        sb.AppendLine();
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                    }
                }
            }

            // Append style setter methods into the same class
            if (!classSymbol.IsGenericType)
            {
                AppendStyleMethods(sb, classSymbol, properties, fullyQualifiedClassName);
            }
            else if (isCRTP)
            {
                AppendCRTPStyleMethods(sb, classSymbol, properties, crtpTypeParam!);
            }

            // ── Event subscription methods ────────────────────────────────────────────
            var eventSymbols = classSymbol
                .GetMembers()
                .OfType<IEventSymbol>()
                .Where(e => e.DeclaredAccessibility == Accessibility.Public
                         && !e.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("NotFluent") == true))
                .ToList();

            if (eventSymbols.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("        // Event subscription methods");

                var evtMethodPrefix = $"public static {methodReturnType}";
                var evtGenericConstraintsString = "";
                if (classSymbol.IsGenericType && genericConstraints.Count > 0)
                {
                    evtGenericConstraintsString = "\n" + string.Join("\n", genericConstraints);
                }

                var handlerDisplayFormat = new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

                foreach (var evt in eventSymbols)
                {
                    string evtName = evt.Name;
                    string methodName = "On" + evtName;
                    string handlerType = evt.Type.ToDisplayString(handlerDisplayFormat);

                    // Primary overload — matches the event delegate type exactly
                    sb.AppendLine();
                    sb.AppendLine($"            /// <summary>Subscribes a handler to the <c>{evtName}</c> event.</summary>");
                    sb.AppendLine($"            {evtMethodPrefix} {methodName}{extensionGenericParams}(this {fullyQualifiedClassName} self, {handlerType} handler){evtGenericConstraintsString}");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                self.{evtName} += handler;");
                    sb.AppendLine($"                return {returnExpression};");
                    sb.AppendLine("            }");

                    // Convenience Action overload for Action<T> events (single type argument only)
                    // Lets callers write   .OnTapped(() => ...)   instead of   .OnTapped(_ => ...)
                    if (IsSingleArgActionType(evt.Type))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"            /// <summary>Subscribes a parameterless handler to the <c>{evtName}</c> event.</summary>");
                        sb.AppendLine($"            {evtMethodPrefix} {methodName}{extensionGenericParams}(this {fullyQualifiedClassName} self, global::System.Action handler){evtGenericConstraintsString}");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                self.{evtName} += _ => handler();");
                        sb.AppendLine($"                return {returnExpression};");
                        sb.AppendLine("            }");
                    }
                }
            }

            // Emit [ModuleInitializer] that registers property effects (Avalonia-style).
            // Only for concrete (non-generic) classes — generic/CRTP classes cannot be
            // registered with typeof() using open type parameters.
            if (!classSymbol.IsGenericType)
            {
                // Only writable, non-collection properties that have a real setter.
                var settableProps = properties
                    .Where(p => p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
                    .ToList();

                var layoutNames = settableProps
                    .Where(p => GetMarkMethod(p) == "MarkNeedsLayout()")
                    .Select(p => $"\"{p.Name}\"")
                    .ToList();

                var paintNames = settableProps
                    .Where(p => GetMarkMethod(p) == "MarkNeedsPaint()")
                    .Select(p => $"\"{p.Name}\"")
                    .ToList();

                if (layoutNames.Count > 0 || paintNames.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("        // Registers which properties trigger layout vs repaint.");
                    sb.AppendLine("        // Called once per assembly load by the runtime.");
                    sb.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
                    sb.AppendLine($"        internal static void RegisterPropertyEffects()");
                    sb.AppendLine("        {");

                    if (layoutNames.Count > 0)
                    {
                        sb.AppendLine($"            global::Rayo.Core.VisualElement.RegisterLayoutProperties(");
                        sb.AppendLine($"                typeof({fullyQualifiedClassName}),");
                        sb.AppendLine($"                {string.Join(", ", layoutNames)});");
                    }

                    if (paintNames.Count > 0)
                    {
                        sb.AppendLine($"            global::Rayo.Core.VisualElement.RegisterPaintProperties(");
                        sb.AppendLine($"                typeof({fullyQualifiedClassName}),");
                        sb.AppendLine($"                {string.Join(", ", paintNames)});");
                    }

                    sb.AppendLine("        }");
                }
            }

            sb.AppendLine("    }");

            // Only close namespace if we opened it
            if (!isGlobalNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Appends Style&lt;T&gt; fluent setter methods into the already-open extensions class.
        /// One method per styleable property; collection/list properties are excluded.
        /// Brush properties get an extra Color overload.
        /// Only called for concrete (non-generic) classes.
        /// </summary>
        private static void AppendStyleMethods(
            StringBuilder sb,
            INamedTypeSymbol classSymbol,
            List<IPropertySymbol> properties,
            string fullyQualifiedClassName)
        {
            var className = classSymbol.Name;
            var styleType = $"global::Rayo.Styling.Style<{fullyQualifiedClassName}>";

            var displayFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                     SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            foreach (var property in properties)
            {
                // Skip collection / read-only-collection / list properties — not meaningful in styles
                if (IsReadOnlyCollectionProperty(property)) continue;
                if (IsListProperty(property, out _)) continue;
                if (IsCollectionType(property.Type, out _)) continue;

                // Skip properties whose setter is not public (e.g. `internal set`)
                if (property.SetMethod == null) continue;
                if (property.SetMethod.DeclaredAccessibility != Accessibility.Public) continue;

                var propertyName = property.Name;
                var propertyType = property.Type.ToDisplayString(displayFormat);
                var propertyTypeFullName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isBrushProperty = propertyTypeFullName == "global::Rayo.Rendering.Brushes.Brush" ||
                    propertyTypeFullName == "global::Rayo.Rendering.Brushes.Brush?";
                var paramName = char.ToLower(propertyName[0]) + propertyName.Substring(1);

                // Primary overload: exact property type
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>Sets the <c>{propertyName}</c> style for all matching <c>{className}</c> elements.</summary>");
                sb.AppendLine($"        public static {styleType} {propertyName}(this {styleType} style, {propertyType} {paramName})");
                sb.AppendLine($"            => style.Set(e => e.{propertyName} = {paramName});");

                // Brush properties also accept Color (implicit conversion, but explicit overload for clarity)
                if (isBrushProperty)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        /// <summary>Sets the <c>{propertyName}</c> style using a solid color for all matching <c>{className}</c> elements.</summary>");
                    sb.AppendLine($"        public static {styleType} {propertyName}(this {styleType} style, global::Rayo.Rendering.Color {paramName})");
                    sb.AppendLine($"            => style.Set(e => e.{propertyName} = {paramName});");
                }
            }
        }

        /// <summary>
        /// Appends Style&lt;T&gt; fluent setter methods for CRTP generic classes into the already-open
        /// extensions class. Each setter is a generic method constrained by the CRTP bound:
        ///   public static Style&lt;T&gt; Fill&lt;T&gt;(this Style&lt;T&gt; style, Brush fill) where T : Shape&lt;T&gt;
        /// This lets callers write <c>new Style&lt;Rectangle&gt;().Fill(color)</c> without needing
        /// a per-subclass generated style class.
        /// </summary>
        private static void AppendCRTPStyleMethods(
            StringBuilder sb,
            INamedTypeSymbol classSymbol,
            List<IPropertySymbol> properties,
            ITypeParameterSymbol crtpTypeParam)
        {
            var className = classSymbol.Name;
            var T = crtpTypeParam.Name;
            var styleType = $"global::Rayo.Styling.Style<{T}>";

            // Build the "where T : ClassName<T>" constraint string
            var crtpConstraintType = crtpTypeParam.ConstraintTypes
                .OfType<INamedTypeSymbol>()
                .First(c => SymbolEqualityComparer.Default.Equals(c.OriginalDefinition, classSymbol.OriginalDefinition));
            var constraintStr = crtpConstraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var displayFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                     SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            foreach (var property in properties)
            {
                // Skip collection / read-only-collection / list properties — not meaningful in styles
                if (IsReadOnlyCollectionProperty(property)) continue;
                if (IsListProperty(property, out _)) continue;
                if (IsCollectionType(property.Type, out _)) continue;

                if (property.SetMethod == null) continue;
                if (property.SetMethod.DeclaredAccessibility != Accessibility.Public) continue;

                var propertyName = property.Name;
                var propertyType = property.Type.ToDisplayString(displayFormat);
                var propertyTypeFullName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isBrushProperty = propertyTypeFullName == "global::Rayo.Rendering.Brushes.Brush" ||
                    propertyTypeFullName == "global::Rayo.Rendering.Brushes.Brush?";
                var paramName = char.ToLower(propertyName[0]) + propertyName.Substring(1);

                sb.AppendLine();
                sb.AppendLine($"        /// <summary>Sets the <c>{propertyName}</c> style for all <c>{className}&lt;T&gt;</c> elements.</summary>");
                sb.AppendLine($"        public static {styleType} {propertyName}<{T}>(this {styleType} style, {propertyType} {paramName})");
                sb.AppendLine($"            where {T} : {constraintStr}");
                sb.AppendLine($"            => style.Set(e => e.{propertyName} = {paramName});");

                if (isBrushProperty)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        /// <summary>Sets the <c>{propertyName}</c> style using a solid color for all <c>{className}&lt;T&gt;</c> elements.</summary>");
                    sb.AppendLine($"        public static {styleType} {propertyName}<{T}>(this {styleType} style, global::Rayo.Rendering.Color {paramName})");
                    sb.AppendLine($"            where {T} : {constraintStr}");
                    sb.AppendLine($"            => style.Set(e => e.{propertyName} = {paramName});");
                }
            }
        }

        /// <summary>
        /// Returns true when <paramref name="type"/> is exactly <c>System.Action&lt;T&gt;</c>
        /// (i.e. a single type-argument generic Action delegate).
        /// Used to decide whether to emit a convenience parameterless overload.
        /// </summary>
        private static bool IsSingleArgActionType(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named) return false;
            if (named.TypeArguments.Length != 1) return false;
            var definition = named.ConstructedFrom.ToDisplayString();
            return definition == "System.Action<T>" || definition == "System.Action`1";
        }
    }
}



