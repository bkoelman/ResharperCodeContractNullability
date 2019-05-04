using System.Collections.Immutable;
using System.Linq;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.SymbolAnalysis;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability
{
    /// <summary>
    /// Entry point for analyzer that converts nullability annotations to C# syntax.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullableReferenceTypeConversionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CNUL";

        [NotNull]
        private readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(DiagnosticId,
            "Resharper nullability annotation can be converted to C# syntax.",
            "Resharper nullability annotation(s) on {0} '{1}' can be converted to C# syntax.", "Nullability",
            DiagnosticSeverity.Warning, true, "Resharper nullability annotation(s) have been superseded by C# built-in syntax.",
            "https://github.com/bkoelman/ResharperCodeContractNullability/blob/master/doc/reference/CNUL_ResharperNullabilityAnnotationsCanBeConvertedToCSharpSyntax.md");

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        [NotNull]
        public ExtensionPoint<INullabilityAttributeProvider> NullabilityAttributeProvider { get; } =
            new ExtensionPoint<INullabilityAttributeProvider>(() => new CachingNullabilityAttributeProvider());

        public override void Initialize([NotNull] AnalysisContext context)
        {
            Guard.NotNull(context, nameof(context));

            context.RegisterCompilationStartAction(StartAnalyzeCompilation);
        }

        private void StartAnalyzeCompilation([NotNull] CompilationStartAnalysisContext context)
        {
            Guard.NotNull(context, nameof(context));

            if (!NullableReferenceTypeSupport.IsActive(context.Compilation))
            {
                return;
            }

            NullabilityAttributeSymbols nullSymbols = NullabilityAttributeProvider.GetCached()
                .GetSymbols(context.Compilation, context.CancellationToken);
            if (nullSymbols == null)
            {
                // Nullability attributes not found; keep silent.
                return;
            }

            context.RegisterSymbolAction(c => AnalyzeSymbol(c, nullSymbols), SymbolKind.Field, SymbolKind.Property,
                SymbolKind.Method, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(c => AnalyzeSymbol(c.ToSymbolContext(), nullSymbols), SyntaxKind.Parameter);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context, [NotNull] NullabilityAttributeSymbols nullSymbols)
        {
            if (context.Symbol.Kind == SymbolKind.NamedType && ((INamedTypeSymbol)context.Symbol).TypeKind != TypeKind.Delegate)
            {
                return;
            }

            bool hasCanBeNull = false;
            bool hasNotNull = false;
            bool hasItemCanBeNull = false;
            bool hasItemNotNull = false;

            foreach (AttributeData attribute in context.Symbol.GetAttributes())
            {
                if (attribute.AttributeClass.Equals(nullSymbols.CanBeNull))
                {
                    hasCanBeNull = true;
                }
                else if (attribute.AttributeClass.Equals(nullSymbols.NotNull))
                {
                    hasNotNull = true;
                }
                else if (attribute.AttributeClass.Equals(nullSymbols.ItemCanBeNull))
                {
                    hasItemCanBeNull = true;
                }
                else if (attribute.AttributeClass.Equals(nullSymbols.ItemNotNull))
                {
                    hasItemNotNull = true;
                }
            }

            bool hasConflictingAnnotation = hasNotNull && hasCanBeNull;
            bool hasConflictingItemAnnotation = hasItemNotNull && hasItemCanBeNull;

            if (hasConflictingAnnotation || hasConflictingItemAnnotation)
            {
                // Conflicting attributes which we cannot convert. User should resolve.
                return;
            }

            if (hasCanBeNull || hasNotNull || hasItemCanBeNull || hasItemNotNull)
            {
                if (context.Symbol is IParameterSymbol parameterSymbol &&
                    parameterSymbol.IsParameterInPartialMethod(context.CancellationToken) &&
                    !ConfirmParameterHasNullabilityAttribute(context, nullSymbols, parameterSymbol))
                {
                    // Parameter in partial method that does not contain any nullability attributes.
                    return;
                }

                string symbolType = GetSymbolTypeText(context);
                Diagnostic diagnostic = Diagnostic.Create(rule, context.Symbol.Locations[0], symbolType, context.Symbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool ConfirmParameterHasNullabilityAttribute(SymbolAnalysisContext context,
            [NotNull] NullabilityAttributeSymbols nullSymbols, [NotNull] IParameterSymbol parameterSymbol)
        {
            INamedTypeSymbol[] nullabilityAttributeSymbols =
            {
                nullSymbols.NotNull,
                nullSymbols.CanBeNull,
                nullSymbols.ItemNotNull,
                nullSymbols.ItemCanBeNull
            };

            foreach (ParameterSyntax parameterSyntax in parameterSymbol.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax(context.CancellationToken)).OfType<ParameterSyntax>())
            {
                SemanticModel model = context.Compilation.GetSemanticModel(parameterSyntax.SyntaxTree);

                foreach (AttributeSyntax attributeSyntax in parameterSyntax.AttributeLists.SelectMany(list => list.Attributes))
                {
                    ISymbol attributeSymbol = model.GetSymbolInfo(attributeSyntax).Symbol?.ContainingType;

                    if (nullabilityAttributeSymbols.Any(x => x.Equals(attributeSymbol)))
                    {
                        // Partial method contains the actual attribute(s).
                        return true;
                    }
                }
            }

            return false;
        }

        [NotNull]
        private static string GetSymbolTypeText(SymbolAnalysisContext context)
        {
            switch (context.Symbol)
            {
                case IFieldSymbol _:
                {
                    return "field";
                }
                case IPropertySymbol _:
                {
                    return "property";
                }
                case IMethodSymbol _:
                {
                    return "method";
                }
                case INamedTypeSymbol typeSymbol when typeSymbol.TypeKind == TypeKind.Delegate:
                {
                    return "delegate";
                }
                case IParameterSymbol _:
                {
                    return "parameter";
                }
            }

            return string.Empty;
        }
    }
}
