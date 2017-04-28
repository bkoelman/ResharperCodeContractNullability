using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of the return value of a method.
    /// </summary>
    public sealed class MethodReturnValueAnalyzer : BaseSymbolAnalyzer<IMethodSymbol>
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

        protected override TypeHierarchyLookupResult GetAnnotationInBaseClass()
        {
            bool higherLevelSeenInSource = false;
            bool higherLevelSeenInAssembly = false;

            IMethodSymbol baseMember = Symbol.OverriddenMethod;
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

                baseMember = baseMember.OverriddenMethod;
            }

            return TypeHierarchyLookupResult.ForNonAnnotated(higherLevelSeenInSource, higherLevelSeenInAssembly);
        }
    }
}
