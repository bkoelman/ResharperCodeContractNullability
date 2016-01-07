using System.Collections.Immutable;
using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs the basic analysis (and reporting) required to determine whether a member or parameter needs annotation.
    /// </summary>
    /// <typeparam name="TSymbol">
    /// The symbol type of the class member to analyze.
    /// </typeparam>
    public abstract class BaseSymbolAnalyzer<TSymbol>
        where TSymbol : class, ISymbol
    {
        private readonly SymbolAnalysisContext context;

        [NotNull]
        private readonly GeneratedCodeDocumentCache generatedCodeCache;

        [NotNull]
        private readonly IExternalAnnotationsResolver externalAnnotations;

        protected BaseSymbolAnalyzer(SymbolAnalysisContext context,
            [NotNull] IExternalAnnotationsResolver externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, bool appliesToItem)
        {
            Guard.NotNull(externalAnnotations, nameof(externalAnnotations));
            Guard.NotNull(generatedCodeCache, nameof(generatedCodeCache));

            this.context = context;
            this.generatedCodeCache = generatedCodeCache;
            this.externalAnnotations = externalAnnotations;
            AppliesToItem = appliesToItem;
            Symbol = (TSymbol) context.Symbol;
        }

        protected bool AppliesToItem { get; }

        [NotNull]
        protected TSymbol Symbol { get; }

        public void Analyze([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            Guard.NotNull(descriptor, nameof(descriptor));
            Guard.NotNull(properties, nameof(properties));

            AnalyzeNullability(descriptor, properties);
        }

        private void AnalyzeNullability([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            if (Symbol.HasNullabilityAnnotation(AppliesToItem))
            {
                return;
            }

            ITypeSymbol symbolType = GetEffectiveSymbolType();
            if (symbolType == null || !symbolType.TypeCanContainNull())
            {
                return;
            }

            if (IsSafeToIgnore())
            {
                return;
            }

            AnalyzeFor(descriptor, properties);
        }

        [CanBeNull]
        private ITypeSymbol GetEffectiveSymbolType()
        {
            ITypeSymbol symbolType = GetSymbolType();

            if (AppliesToItem)
            {
                symbolType = symbolType.TryGetItemTypeForSequenceOrCollection(context.Compilation) ??
                    symbolType.TryGetItemTypeForLazyOrGenericTask(context.Compilation);
            }

            return symbolType;
        }

        private bool IsSafeToIgnore()
        {
            if (Symbol.HasCompilerGeneratedAnnotation(context.Compilation) ||
                Symbol.HasDebuggerNonUserCodeAnnotation(context.Compilation) || Symbol.IsImplicitlyDeclared)
            {
                return true;
            }

            if (Symbol.HasResharperConditionalAnnotation(context.Compilation))
            {
                return true;
            }

            if (generatedCodeCache.IsInGeneratedCodeDocument(Symbol, context.CancellationToken))
            {
                return true;
            }

            return false;
        }

        private void AnalyzeFor([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            if (externalAnnotations.HasAnnotationForSymbol(Symbol, AppliesToItem, context.Compilation))
            {
                return;
            }

            if (RequiresAnnotation())
            {
                Diagnostic diagnostic = CreateDiagnosticFor(descriptor, properties);
                context.ReportDiagnostic(diagnostic);
            }
        }

        protected virtual bool RequiresAnnotation()
        {
            if (HasAnnotationInInterface(Symbol))
            {
                // Resharper reports nullability attribute as unneeded on interface implementation
                // if member on interface contains nullability attribute.
                return false;
            }

            if (HasAnnotationInBaseClass())
            {
                // Resharper reports nullability attribute as unneeded 
                // if member on base class contains nullability attribute.
                return false;
            }

            return true;
        }

        [NotNull]
        private Diagnostic CreateDiagnosticFor([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            return Diagnostic.Create(descriptor, Symbol.Locations[0], properties, Symbol.Name);
        }

        protected virtual bool HasAnnotationInInterface([NotNull] TSymbol symbol)
        {
            foreach (INamedTypeSymbol iface in symbol.ContainingType.AllInterfaces)
            {
                foreach (TSymbol ifaceMember in iface.GetMembers().OfType<TSymbol>())
                {
                    ISymbol implementer = symbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    if (symbol.Equals(implementer))
                    {
                        if (ifaceMember.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(ifaceMember))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected virtual bool HasAnnotationInBaseClass()
        {
            return false;
        }

        [NotNull]
        protected abstract ITypeSymbol GetSymbolType();

        protected bool HasExternalAnnotationFor([NotNull] ISymbol symbol)
        {
            return externalAnnotations.HasAnnotationForSymbol(symbol, AppliesToItem, context.Compilation);
        }
    }
}