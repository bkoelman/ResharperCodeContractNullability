using System.Collections.Immutable;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs the basic analysis (and reporting) required to determine whether a member needs annotation.
    /// </summary>
    /// <typeparam name="TSymbol">
    /// The symbol type of the class member to analyze.
    /// </typeparam>
    public abstract class MemberAnalyzer<TSymbol>
        where TSymbol : ISymbol
    {
        private readonly SymbolAnalysisContext context;

        [NotNull]
        private readonly GeneratedCodeDocumentCache generatedCodeCache;

        protected MemberAnalyzer(SymbolAnalysisContext context, [NotNull] ExternalAnnotationsMap externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, bool appliesToItem)
        {
            Guard.NotNull(externalAnnotations, nameof(externalAnnotations));
            Guard.NotNull(generatedCodeCache, nameof(generatedCodeCache));

            this.context = context;
            this.generatedCodeCache = generatedCodeCache;
            ExternalAnnotations = externalAnnotations;
            AppliesToItem = appliesToItem;
            Symbol = (TSymbol) (context.Symbol.OriginalDefinition ?? context.Symbol);
        }

        [NotNull]
        protected ExternalAnnotationsMap ExternalAnnotations { get; }

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
            bool canBeSkipped = AppliesToItem ? CanItemBeSkipped() : CanBeSkipped();
            if (!canBeSkipped)
            {
                AnalyzeFor(descriptor, properties);
            }
        }

        private void AnalyzeFor([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            if (ExternalAnnotations.Contains(Symbol, AppliesToItem))
            {
                return;
            }

            if (RequiresAnnotation())
            {
                Diagnostic diagnostic = CreateDiagnosticFor(descriptor, properties);
                context.ReportDiagnostic(diagnostic);
            }
        }

        [NotNull]
        private Diagnostic CreateDiagnosticFor([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            return Diagnostic.Create(descriptor, Symbol.Locations[0], properties, Symbol.Name);
        }

        protected virtual bool HasAnnotationInInterface()
        {
            foreach (INamedTypeSymbol iface in Symbol.ContainingType.AllInterfaces)
            {
                foreach (TSymbol ifaceMember in iface.GetMembers().OfType<TSymbol>())
                {
                    ISymbol implementer = Symbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (implementer == (ISymbol) Symbol)
                    {
                        bool defined = AppliesToItem
                            ? ifaceMember.HasItemNullabilityDefined()
                            : ifaceMember.HasNullabilityDefined();
                        if (defined || ExternalAnnotations.Contains(ifaceMember, AppliesToItem))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool CanBeSkipped()
        {
            if (Symbol.HasNullabilityDefined())
            {
                return true;
            }

            ITypeSymbol symbolType = GetSymbolType();
            if (!symbolType.TypeCanContainNull())
            {
                return true;
            }

            return IsSafeToIgnore();
        }

        private bool CanItemBeSkipped()
        {
            if (Symbol.HasItemNullabilityDefined())
            {
                return true;
            }

            ITypeSymbol symbolType = GetSymbolType();

            ITypeSymbol itemSymbolType = symbolType.TryGetItemTypeForSequenceOrCollection(context.Compilation);
            if (itemSymbolType == null)
            {
                return true;
            }

            if (!itemSymbolType.TypeCanContainNull())
            {
                return true;
            }

            return IsSafeToIgnore();
        }

        private bool IsSafeToIgnore()
        {
            if (Symbol.IsCompilerGenerated(context.Compilation) || Symbol.IsDebuggerNonUserCode(context.Compilation) ||
                Symbol.IsImplicitlyDeclared)
            {
                return true;
            }

            if (Symbol.IsAnnotatedWithReshaperConditionalAttribute(context.Compilation))
            {
                return true;
            }

            if (generatedCodeCache.IsInGeneratedCodeDocument(Symbol, context.CancellationToken))
            {
                return true;
            }

            return false;
        }

        [NotNull]
        protected abstract ITypeSymbol GetSymbolType();

        protected abstract bool RequiresAnnotation();
    }
}