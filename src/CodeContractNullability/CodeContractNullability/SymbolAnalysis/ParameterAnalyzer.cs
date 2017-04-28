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

        protected override TypeHierarchyLookupResult GetAnnotationInBaseClass()
        {
            bool higherLevelSeenInSource = false;
            bool higherLevelSeenInAssembly = false;

            IParameterSymbol baseParameter = TryGetBaseParameterFor(Symbol);
            while (baseParameter != null)
            {
                bool isInExternalAssembly = baseParameter.IsInExternalAssembly();
                if (isInExternalAssembly)
                {
                    higherLevelSeenInAssembly = true;
                }
                else
                {
                    higherLevelSeenInSource = true;
                }

                if (baseParameter.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(baseParameter))
                {
                    return TypeHierarchyLookupResult.ForAnnotated(isInExternalAssembly);
                }

                TypeHierarchyLookupResult interfaceLookupResult = GetAnnotationInInterface(baseParameter);
                if (interfaceLookupResult.HasAnnotation)
                {
                    return interfaceLookupResult;
                }

                baseParameter = TryGetBaseParameterFor(baseParameter);
            }

            return TypeHierarchyLookupResult.ForNonAnnotated(higherLevelSeenInSource, higherLevelSeenInAssembly);
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

        protected override TypeHierarchyLookupResult GetAnnotationInInterface(IParameterSymbol parameter)
        {
            bool higherLevelSeenInSource = false;
            bool higherLevelSeenInAssembly = false;

            ISymbol containingMember = parameter.ContainingSymbol;

            foreach (INamedTypeSymbol iface in parameter.ContainingType.AllInterfaces)
            {
                foreach (ISymbol ifaceMember in iface.GetMembers())
                {
                    ISymbol implementer = parameter.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    if (containingMember.Equals(implementer))
                    {
                        bool isInExternalAssembly = ifaceMember.IsInExternalAssembly();
                        if (isInExternalAssembly)
                        {
                            higherLevelSeenInAssembly = true;
                        }
                        else
                        {
                            higherLevelSeenInSource = true;
                        }

                        ImmutableArray<IParameterSymbol> parameters = GetParametersFor(containingMember);
                        int parameterIndex = parameters.IndexOf(parameter);

                        ImmutableArray<IParameterSymbol> ifaceParameters = GetParametersFor(ifaceMember);
                        IParameterSymbol ifaceParameter = ifaceParameters[parameterIndex];

                        if (ifaceParameter.HasNullabilityAnnotation(AppliesToItem) || HasExternalAnnotationFor(ifaceParameter))
                        {
                            return TypeHierarchyLookupResult.ForAnnotated(isInExternalAssembly);
                        }
                    }
                }
            }

            return TypeHierarchyLookupResult.ForNonAnnotated(higherLevelSeenInSource, higherLevelSeenInAssembly);
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
