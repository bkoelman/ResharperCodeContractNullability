using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of a property.
    /// </summary>
    public sealed class PropertyAnalyzer : BaseSymbolAnalyzer<IPropertySymbol>
    {
        public PropertyAnalyzer(SymbolAnalysisContext context, [NotNull] AnalysisScope scope)
            : base(context, scope)
        {
        }

        protected override ITypeSymbol GetSymbolType()
        {
            return Symbol.Type;
        }

        protected override TypeHierarchyLookupResult GetAnnotationInBaseClass()
        {
            bool higherLevelSeenInSource = false;
            bool higherLevelSeenInAssembly = false;

            IPropertySymbol baseMember = Symbol.OverriddenProperty;
            while (baseMember != null)
            {
                bool isInExternalAssembly = baseMember.IsInExternalAssembly();
                if (isInExternalAssembly)
                {
                    higherLevelSeenInAssembly = true;
                }
                else
                {
                    higherLevelSeenInSource = true;
                }

                if (baseMember.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(baseMember))
                {
                    return TypeHierarchyLookupResult.ForAnnotated(isInExternalAssembly);
                }

                TypeHierarchyLookupResult interfaceLookupResult = GetAnnotationInInterface(baseMember);
                if (interfaceLookupResult.HasAnnotation)
                {
                    return interfaceLookupResult;
                }

                baseMember = baseMember.OverriddenProperty;
            }

            return TypeHierarchyLookupResult.ForNonAnnotated(higherLevelSeenInSource, higherLevelSeenInAssembly);
        }
    }
}
