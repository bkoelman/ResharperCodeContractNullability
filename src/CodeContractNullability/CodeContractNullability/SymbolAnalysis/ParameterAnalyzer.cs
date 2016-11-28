using System;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of a method parameter.
    /// </summary>
    public sealed class ParameterAnalyzer : BaseSymbolAnalyzer<IParameterSymbol>
    {
        public ParameterAnalyzer(SymbolAnalysisContext context, [NotNull] AnalysisScope scope)
            : base(context, scope)
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
                if (baseParameter.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(baseParameter) ||
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

                    if (containingMember.Equals(implementer))
                    {
                        ImmutableArray<IParameterSymbol> parameters = GetParametersFor(containingMember);
                        int parameterIndex = parameters.IndexOf(parameter);

                        ImmutableArray<IParameterSymbol> ifaceParameters = GetParametersFor(ifaceMember);
                        IParameterSymbol ifaceParameter = ifaceParameters[parameterIndex];

                        if (ifaceParameter.HasNullabilityAnnotation(AppliesToItem) ||
                            HasExternalAnnotationFor(ifaceParameter))
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
