using System;
using System.Collections.Immutable;
using CodeContractNullability.Settings;
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
            TypeHierarchyLookupResult lookupResult = GetAnnotationInTypeHierarchy();

            // Truth table which determines per report mode if annotation is required.
            // For example, "YX" means: lookupResult = { Source = true, Assembly = null }
            //
            //  mode:       Always  Highest Top
            //  XX          Y       Y       Y
            //  XN          Y       Y       N
            //  XY          N       N       N
            //  NX          Y       N       N
            //  NN          Y       N       N
            //  NY          N       N       N
            //  YX          N       N       N
            //  YN          N       N       N
            //  YY          N       N       N

            switch (scope.Settings.TypeHierarchyReportMode)
            {
                case TypeHierarchyReportMode.Always:
                    return !(lookupResult.IsAnnotatedAtHigherLevelInSource == true ||
                        lookupResult.IsAnnotatedAtHigherLevelInAssembly == true);
                case TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy:
                    return lookupResult.IsAnnotatedAtHigherLevelInSource == null &&
                        lookupResult.IsAnnotatedAtHigherLevelInAssembly != true;
                case TypeHierarchyReportMode.AtTopInTypeHierarchy:
                    return lookupResult.IsAnnotatedAtHigherLevelInSource == null &&
                        lookupResult.IsAnnotatedAtHigherLevelInAssembly == null;
                default:
                    throw new NotSupportedException($"Unsupported mode '{scope.Settings.TypeHierarchyReportMode}'.");
            }
        }

        private TypeHierarchyLookupResult GetAnnotationInTypeHierarchy()
        {
            // Resharper reports nullability attribute as unneeded on implemented interface member
            // if member on interface contains nullability attribute.
            TypeHierarchyLookupResult interfaceLookupResult = GetAnnotationInInterface(Symbol);

            // Resharper reports nullability attribute as unneeded on derived member
            // if member on base class contains nullability attribute.
            TypeHierarchyLookupResult baseClassLookupResult = GetAnnotationInBaseClass();

            return TypeHierarchyLookupResult.Merge(interfaceLookupResult, baseClassLookupResult);
        }

        [NotNull]
        private Diagnostic CreateDiagnosticFor([NotNull] DiagnosticDescriptor descriptor,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            return Diagnostic.Create(descriptor, Symbol.Locations[0], properties, Symbol.Name);
        }

        protected virtual TypeHierarchyLookupResult GetAnnotationInInterface([NotNull] TSymbol symbol)
        {
            bool higherLevelSeenInSource = false;
            bool higherLevelSeenInAssembly = false;

            foreach (INamedTypeSymbol iface in symbol.ContainingType.AllInterfaces)
            {
                foreach (TSymbol ifaceMember in iface.GetMembers().OfType<TSymbol>())
                {
                    ISymbol implementer = symbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    if (symbol.Equals(implementer))
                    {
                        bool isInExternalAssembly = ifaceMember.IsInExternalAssembly();
                        if (isInExternalAssembly)
                        {
                            higherLevelSeenInAssembly = true;
                        }
                        else
                        {
                            higherLevelSeenInSource = true;
                        }

                        if (ifaceMember.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(ifaceMember))
                        {
                            return TypeHierarchyLookupResult.ForAnnotated(isInExternalAssembly);
                        }
                    }
                }
            }

            return TypeHierarchyLookupResult.ForNonAnnotated(higherLevelSeenInSource, higherLevelSeenInAssembly);
        }

        protected virtual TypeHierarchyLookupResult GetAnnotationInBaseClass()
        {
            return TypeHierarchyLookupResult.ForNonAnnotated(false, false);
        }

        [NotNull]
        protected abstract ITypeSymbol GetSymbolType();

        protected bool HasExternalAnnotationFor([NotNull] ISymbol symbol)
        {
            return scope.ExternalAnnotations.HasAnnotationForSymbol(symbol, AppliesToItem, context.Compilation);
        }
    }
}
