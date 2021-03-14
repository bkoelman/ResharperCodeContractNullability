using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of a property.
    /// </summary>
    internal sealed class PropertyAnalyzer : BaseSymbolAnalyzer<IPropertySymbol>
    {
        public PropertyAnalyzer(SymbolAnalysisContext context, [NotNull] AnalysisScope scope)
            : base(context, scope)
        {
        }

        protected override ITypeSymbol GetSymbolType()
        {
            return Symbol.Type;
        }

        protected override bool HasAnnotationInBaseClass()
        {
            IPropertySymbol baseMember = Symbol.OverriddenProperty;

            while (baseMember != null)
            {
                if (baseMember.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(baseMember) || HasAnnotationInInterface(baseMember))
                {
                    return true;
                }

                baseMember = baseMember.OverriddenProperty;
            }

            return false;
        }
    }
}
