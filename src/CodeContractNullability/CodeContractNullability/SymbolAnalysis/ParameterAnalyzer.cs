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
    internal sealed class ParameterAnalyzer : BaseSymbolAnalyzer<IParameterSymbol>
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
            return Symbol.ContainingSymbol is IMethodSymbol method && FunctionAnalysis.KindsToSkip.Contains(method.MethodKind);
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

            foreach (INamedTypeSymbol @interface in parameter.ContainingType.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in @interface.GetMembers())
                {
                    ISymbol implementer = parameter.ContainingType.FindImplementationForInterfaceMember(interfaceMember);

                    if (containingMember.Equals(implementer))
                    {
                        ImmutableArray<IParameterSymbol> parameters = GetParametersFor(containingMember);
                        int parameterIndex = parameters.IndexOf(parameter);

                        ImmutableArray<IParameterSymbol> interfaceParameters = GetParametersFor(interfaceMember);
                        IParameterSymbol interfaceParameter = interfaceParameters[parameterIndex];

                        if (interfaceParameter.HasNullabilityAnnotation(AppliesToItem) ||
                            HasExternalAnnotationFor(interfaceParameter))
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
            if (symbol is IMethodSymbol method)
            {
                return method.Parameters;
            }

            if (symbol is IPropertySymbol property)
            {
                return property.Parameters;
            }

            throw new NotSupportedException($"Expected IMethodSymbol or IPropertySymbol, not {symbol.GetType()}.");
        }
    }
}
