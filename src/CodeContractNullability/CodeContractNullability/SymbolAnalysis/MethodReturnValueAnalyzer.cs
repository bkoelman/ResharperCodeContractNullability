using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of the return value of a method.
    /// </summary>
    internal sealed class MethodReturnValueAnalyzer : BaseSymbolAnalyzer<IMethodSymbol>
    {
        public MethodReturnValueAnalyzer(SymbolAnalysisContext context, [NotNull] AnalysisScope scope)
            : base(context, scope)
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

            if (!AppliesToItem && Symbol.IsAsync)
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
                if (baseMember.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(baseMember) ||
                    HasAnnotationInInterface(baseMember))
                {
                    return true;
                }

                baseMember = baseMember.OverriddenMethod;
            }

            return false;
        }
    }
}
