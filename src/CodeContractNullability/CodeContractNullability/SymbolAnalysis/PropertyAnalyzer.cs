using CodeContractNullability.ExternalAnnotations.Storage;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    public class PropertyAnalyzer : MemberAnalyzer<IPropertySymbol>
    {
        public PropertyAnalyzer(SymbolAnalysisContext context, [NotNull] ExternalAnnotationsMap externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, bool appliesToItem)
            : base(context, externalAnnotations, generatedCodeCache, appliesToItem)
        {
        }

        protected override ITypeSymbol GetSymbolType()
        {
            return Symbol.Type;
        }

        protected override bool RequiresAnnotation()
        {
            if (HasAnnotationInBaseClass(Symbol))
            {
                // Resharper reports nullability attribute as unneeded 
                // if property on base class contains nullability attribute.
                return false;
            }

            if (HasAnnotationInInterface())
            {
                // Resharper reports nullability attribute as unneeded on interface implementation
                // if property on interface contains nullability attribute.
                return false;
            }

            return true;
        }

        private bool HasAnnotationInBaseClass([NotNull] IPropertySymbol propertySymbol)
        {
            IPropertySymbol baseMember = propertySymbol.OverriddenProperty;
            while (baseMember != null)
            {
                bool defined = baseMember.HasNullabilityAnnotation(AppliesToItem);
                if (defined || ExternalAnnotations.Contains(baseMember, AppliesToItem))
                {
                    return true;
                }

                baseMember = baseMember.OverriddenProperty;
            }
            return false;
        }
    }
}