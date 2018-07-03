using System.Collections.Immutable;
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
    internal abstract class BaseSymbolAnalyzer<TSymbol>
        where TSymbol : class, ISymbol
    {
        private SymbolAnalysisContext context;

        [NotNull]
        private readonly AnalysisScope scope;

        [NotNull]
        protected TSymbol Symbol { get; }

        protected bool AppliesToItem => scope.AppliesToItem;

        protected BaseSymbolAnalyzer(SymbolAnalysisContext context, [NotNull] AnalysisScope scope)
        {
            Guard.NotNull(scope, nameof(scope));

            this.context = context;
            this.scope = scope;

            Symbol = (TSymbol)this.context.Symbol;
        }

        public void Analyze([NotNull] DiagnosticDescriptor descriptor, [NotNull] ImmutableDictionary<string, string> properties)
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
            if (symbolType == null || !symbolType.TypeCanContainNull(scope.Settings.DisableReportOnNullableValueTypes))
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
                symbolType = symbolType.TryGetItemTypeForSequenceOrCollection(scope.TypeCache) ??
                    symbolType.TryGetItemTypeForLazyOrGenericTask(scope.TypeCache);
            }

            return symbolType;
        }

        private bool IsSafeToIgnore()
        {
            if (Symbol.HasCompilerGeneratedAnnotation(scope.TypeCache) ||
                Symbol.HasDebuggerNonUserCodeAnnotation(scope.TypeCache) || Symbol.IsImplicitlyDeclared)
            {
                return true;
            }

            if (Symbol.HasResharperConditionalAnnotation(scope.TypeCache))
            {
                return true;
            }

            if (scope.GeneratedCodeCache.IsInGeneratedCodeDocument(Symbol, context.CancellationToken))
            {
                return true;
            }

            return false;
        }

        private void AnalyzeFor([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            if (scope.ExternalAnnotations.HasAnnotationForSymbol(Symbol, AppliesToItem, context.Compilation))
            {
                return;
            }

            if (RequiresAnnotation())
            {
                Diagnostic nullabilityDiagnostic = CreateDiagnosticFor(descriptor, properties);
                context.ReportDiagnostic(nullabilityDiagnostic);

                if (ShouldSuggestDisableReportOnNullableValueTypes())
                {
                    Diagnostic disableDiagnostic = CreateDiagnosticFor(scope.DisableReportOnNullableValueTypesRule,
                        ImmutableDictionary<string, string>.Empty);
                    context.ReportDiagnostic(disableDiagnostic);
                }
            }
        }

        private bool ShouldSuggestDisableReportOnNullableValueTypes()
        {
            if (!scope.Settings.DisableReportOnNullableValueTypes)
            {
                ITypeSymbol symbolType = GetEffectiveSymbolType();
                if (symbolType != null && symbolType.IsSystemNullableType())
                {
                    return true;
                }
            }

            return false;
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
            return scope.ExternalAnnotations.HasAnnotationForSymbol(symbol, AppliesToItem, context.Compilation);
        }
    }
}
