using CodeContractNullability.ExternalAnnotations.Storage;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of the return value of a method.
    /// </summary>
    public class MethodReturnValueAnalyzer : BaseSymbolAnalyzer<IMethodSymbol>
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
            if (FunctionAnalysis.KindsToSkip.Contains(Symbol.MethodKind))
            {
                return false;
            }

            return base.RequiresAnnotation();
        }

        protected override bool HasAnnotationInBaseClass()
        {
            IMethodSymbol baseMember = Symbol.OverriddenMethod;
            while (baseMember != null)
            {
                if (baseMember.HasNullabilityAnnotation(AppliesToItem) ||
                    ExternalAnnotations.Contains(baseMember, AppliesToItem) || HasAnnotationInInterface(baseMember))
                {
                    return true;
                }

                baseMember = baseMember.OverriddenMethod;
            }
            return false;
        }
    }
}