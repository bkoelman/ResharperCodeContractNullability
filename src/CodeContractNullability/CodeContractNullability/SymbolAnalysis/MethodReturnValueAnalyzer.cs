using CodeContractNullability.ExternalAnnotations.Storage;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    public class MethodReturnValueAnalyzer : MemberAnalyzer<IMethodSymbol>
    {
        public MethodReturnValueAnalyzer(SymbolAnalysisContext context,
            [NotNull] ExternalAnnotationsMap externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, bool appliesToItem)
            : base(context, externalAnnotations, generatedCodeCache, appliesToItem)
        {
        }

        protected override ITypeSymbol GetSymbolType()
        {
            return Symbol.ReturnType;
        }

        protected override bool RequiresAnnotation()
        {
            if (MemberAnalyzer.MethodKindsToSkip.Contains(Symbol.MethodKind))
            {
                return false;
            }

            if (HasAnnotationInBaseClass(Symbol))
            {
                // Resharper reports nullability attribute as unneeded 
                // if return value on base method contains nullability attribute.
                return false;
            }

            if (HasAnnotationInInterface())
            {
                // Resharper reports nullability attribute as unneeded on interface implementation
                // if return value on interface method contains nullability attribute.
                return false;
            }

            return true;
        }

        private bool HasAnnotationInBaseClass([NotNull] IMethodSymbol methodSymbol)
        {
            IMethodSymbol baseMember = methodSymbol.OverriddenMethod;
            while (baseMember != null)
            {
                bool defined = baseMember.HasNullabilityAnnotation(AppliesToItem);
                if (defined || ExternalAnnotations.Contains(baseMember, AppliesToItem))
                {
                    return true;
                }

                baseMember = baseMember.OverriddenMethod;
            }
            return false;
        }
    }
}