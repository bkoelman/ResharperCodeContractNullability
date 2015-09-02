using System;
using System.Collections.Immutable;
using CodeContractNullability.ExternalAnnotations.Storage;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    public class ParameterAnalyzer : MemberAnalyzer<IParameterSymbol>
    {
        public ParameterAnalyzer(SymbolAnalysisContext context, [NotNull] ExternalAnnotationsMap externalAnnotations,
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
            if (ContainerIsLambda())
            {
                return false;
            }

            return base.RequiresAnnotation();
        }

        private bool ContainerIsLambda()
        {
            var method = Symbol.ContainingSymbol as IMethodSymbol;
            return method != null && FunctionAnalysis.KindsToSkip.Contains(method.MethodKind);
        }

        protected override bool HasAnnotationInBaseClass()
        {
            IParameterSymbol baseParameter = TryGetBaseParameterFor(Symbol);
            while (baseParameter != null)
            {
                if (baseParameter.HasNullabilityAnnotation(AppliesToItem) ||
                    ExternalAnnotations.Contains(baseParameter, AppliesToItem) ||
                    HasAnnotationInInterface(baseParameter))
                {
                    return true;
                }

                baseParameter = TryGetBaseParameterFor(baseParameter);
            }
            return false;
        }

        [CanBeNull]
        private IParameterSymbol TryGetBaseParameterFor([NotNull] IParameterSymbol parameterSymbol)
        {
            var containingMethod = parameterSymbol.ContainingSymbol as IMethodSymbol;
            IMethodSymbol baseMethod = containingMethod?.OverriddenMethod;
            if (baseMethod != null)
            {
                int parameterIndex = containingMethod.Parameters.IndexOf(parameterSymbol);
                return baseMethod.Parameters[parameterIndex];
            }

            var containingProperty = parameterSymbol.ContainingSymbol as IPropertySymbol;
            IPropertySymbol baseProperty = containingProperty?.OverriddenProperty;
            if (baseProperty != null)
            {
                int parameterIndex = containingProperty.Parameters.IndexOf(parameterSymbol);
                return baseProperty.Parameters[parameterIndex];
            }

            return null;
        }

        protected override bool HasAnnotationInInterface(IParameterSymbol parameter)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            foreach (INamedTypeSymbol iface in parameter.ContainingType.AllInterfaces)
            {
                foreach (ISymbol ifaceMember in iface.GetMembers())
                {
                    ISymbol implementer = parameter.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (implementer == containingMember)
                    {
                        ImmutableArray<IParameterSymbol> parameters = GetParametersFor(containingMember);
                        int parameterIndex = parameters.IndexOf(parameter);

                        ImmutableArray<IParameterSymbol> ifaceParameters = GetParametersFor(ifaceMember);
                        IParameterSymbol ifaceParameter = ifaceParameters[parameterIndex];

                        if (ifaceParameter.HasNullabilityAnnotation(AppliesToItem) ||
                            ExternalAnnotations.Contains(ifaceParameter, AppliesToItem))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        [ItemNotNull]
        private ImmutableArray<IParameterSymbol> GetParametersFor([NotNull] ISymbol symbol)
        {
            var method = symbol as IMethodSymbol;
            if (method != null)
            {
                return method.Parameters;
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                return property.Parameters;
            }

            throw new NotSupportedException($"Expected IMethodSymbol or IPropertySymbol, not {symbol.GetType()}.");
        }
    }
}