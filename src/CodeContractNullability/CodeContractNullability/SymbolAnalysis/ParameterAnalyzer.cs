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

            if (HasAnnotationInBaseClass(Symbol))
            {
                // Resharper reports nullability attribute as unneeded 
                // if parameter on base class contains nullability attribute.
                return false;
            }

            if (HasAnnotationInInterface())
            {
                // Resharper reports nullability attribute as unneeded on interface implementation
                // if parameter on interface contains nullability attribute.
                return false;
            }

            return true;
        }

        private bool ContainerIsLambda()
        {
            var method = Symbol.ContainingSymbol as IMethodSymbol;
            return method != null && FunctionAnalysis.KindsToSkip.Contains(method.MethodKind);
        }

        private bool HasAnnotationInBaseClass([NotNull] IParameterSymbol parameterSymbol)
        {
            IParameterSymbol baseParameter = TryGetBaseParameterFor(parameterSymbol);
            while (baseParameter != null)
            {
                bool defined = baseParameter.HasNullabilityAnnotation(AppliesToItem);
                if (defined || ExternalAnnotations.Contains(baseParameter, AppliesToItem))
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

        protected override bool HasAnnotationInInterface()
        {
            var containingMethod = Symbol.ContainingSymbol as IMethodSymbol;
            if (containingMethod != null)
            {
                return HasAnnotationInInterfaceForParent(containingMethod);
            }

            var containingProperty = Symbol.ContainingSymbol as IPropertySymbol;
            if (containingProperty != null)
            {
                return HasAnnotationInInterfaceForParent(containingProperty);
            }

            return false;
        }

        private bool HasAnnotationInInterfaceForParent([NotNull] IMethodSymbol containingMethod)
        {
            foreach (INamedTypeSymbol iface in Symbol.ContainingType.AllInterfaces)
            {
                foreach (IMethodSymbol ifaceMember in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    ISymbol implementer = Symbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (implementer == containingMethod)
                    {
                        int parameterIndex = containingMethod.Parameters.IndexOf(Symbol);
                        IParameterSymbol ifaceParameter = ifaceMember.Parameters[parameterIndex];

                        bool defined = ifaceParameter.HasNullabilityAnnotation(AppliesToItem);
                        if (defined || ExternalAnnotations.Contains(ifaceParameter, AppliesToItem))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool HasAnnotationInInterfaceForParent([NotNull] IPropertySymbol containingProperty)
        {
            foreach (INamedTypeSymbol iface in Symbol.ContainingType.AllInterfaces)
            {
                foreach (IPropertySymbol ifaceMember in iface.GetMembers().OfType<IPropertySymbol>())
                {
                    ISymbol implementer = Symbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (implementer == containingProperty)
                    {
                        int parameterIndex = containingProperty.Parameters.IndexOf(Symbol);
                        IParameterSymbol ifaceParameter = ifaceMember.Parameters[parameterIndex];

                        bool defined = ifaceParameter.HasNullabilityAnnotation(AppliesToItem);
                        if (defined || ExternalAnnotations.Contains(ifaceParameter, AppliesToItem))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}