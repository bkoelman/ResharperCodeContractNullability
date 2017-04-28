using System.Collections.Immutable;
using System.Linq;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
    public static class SymbolExtensions
    {
        public static bool HasNullabilityAnnotation([NotNull] this ISymbol memberSymbol, bool appliesToItem)
        {
            Guard.NotNull(memberSymbol, nameof(memberSymbol));

            ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();
            return attributes.Any(x => appliesToItem ? IsItemNullabilityAttribute(x) : IsNullabilityAttribute(x));
        }

        private static bool IsNullabilityAttribute([NotNull] AttributeData attribute)
        {
            string typeName = attribute.AttributeClass.Name;
            return typeName == "CanBeNullAttribute" || typeName == "NotNullAttribute";
        }

        private static bool IsItemNullabilityAttribute([NotNull] AttributeData attribute)
        {
            string typeName = attribute.AttributeClass.Name;
            return typeName == "ItemCanBeNullAttribute" || typeName == "ItemNotNullAttribute";
        }

        public static bool TypeCanContainNull([NotNull] this ITypeSymbol typeSymbol, bool disableReportOnNullableValueTypes)
        {
            Guard.NotNull(typeSymbol, nameof(typeSymbol));

            if (IsVoidType(typeSymbol))
            {
                return false;
            }

            if (IsSystemNullableType(typeSymbol))
            {
                return !disableReportOnNullableValueTypes;
            }

            return !typeSymbol.IsValueType;
        }

        private static bool IsVoidType([NotNull] ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType == SpecialType.System_Void;
        }

        private static bool IsSystemNullableType([NotNull] this ITypeSymbol typeSymbol)
        {
            return typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        [CanBeNull]
        public static ITypeSymbol TryGetItemTypeForSequenceOrCollection([NotNull] this ITypeSymbol typeSymbol,
            [NotNull] FrameworkTypeCache typeCache)
        {
            Guard.NotNull(typeSymbol, nameof(typeSymbol));
            Guard.NotNull(typeCache, nameof(typeCache));

            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

            if (typeCache.EnumerableOfT != null)
            {
                foreach (INamedTypeSymbol type in typeSymbol.AllInterfaces.PrependIfNotNull(namedTypeSymbol))
                {
                    if (typeCache.EnumerableOfT.Equals(type.ConstructedFrom))
                    {
                        return type.TypeArguments.Single();
                    }
                }
            }

            if (typeCache.Enumerable != null)
            {
                if (typeSymbol.AllInterfaces.PrependIfNotNull(namedTypeSymbol).Any(type => typeCache.Enumerable.Equals(type)))
                {
                    if (!typeSymbol.Equals(typeCache.String))
                    {
                        return typeCache.Object;
                    }
                }
            }

            return null;
        }

        [CanBeNull]
        public static ITypeSymbol TryGetItemTypeForLazyOrGenericTask([NotNull] this ITypeSymbol typeSymbol,
            [NotNull] FrameworkTypeCache typeCache)
        {
            Guard.NotNull(typeSymbol, nameof(typeSymbol));
            Guard.NotNull(typeCache, nameof(typeCache));

            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
            if (namedTypeSymbol?.ConstructedFrom != null)
            {
                bool isMatch = namedTypeSymbol.ConstructedFrom.Equals(typeCache.LazyOfT) ||
                    namedTypeSymbol.ConstructedFrom.Equals(typeCache.TaskOfT) ||
                    namedTypeSymbol.ConstructedFrom.Equals(typeCache.ValueTaskOfT);

                if (isMatch)
                {
                    return namedTypeSymbol.TypeArguments.Single();
                }
            }

            return null;
        }

        public static bool HasCompilerGeneratedAnnotation([NotNull] this ISymbol memberSymbol,
            [NotNull] FrameworkTypeCache typeCache)
        {
            Guard.NotNull(memberSymbol, nameof(memberSymbol));
            Guard.NotNull(typeCache, nameof(typeCache));

            if (typeCache.CompilerGeneratedAttribute != null)
            {
                ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();
                return attributes.Any(attr => typeCache.CompilerGeneratedAttribute.Equals(attr.AttributeClass));
            }

            return false;
        }

        public static bool HasDebuggerNonUserCodeAnnotation([NotNull] this ISymbol memberSymbol,
            [NotNull] FrameworkTypeCache typeCache)
        {
            Guard.NotNull(memberSymbol, nameof(memberSymbol));
            Guard.NotNull(typeCache, nameof(typeCache));

            if (typeCache.DebuggerNonUserCodeAttribute != null)
            {
                ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();
                return attributes.Any(attr => typeCache.DebuggerNonUserCodeAttribute.Equals(attr.AttributeClass));
            }

            return false;
        }

        public static bool HasResharperConditionalAnnotation([NotNull] this ISymbol symbol,
            [NotNull] FrameworkTypeCache typeCache)
        {
            Guard.NotNull(symbol, nameof(symbol));
            Guard.NotNull(typeCache, nameof(typeCache));

            ImmutableArray<AttributeData> attributes = symbol.ContainingType.GetAttributes();
            return attributes.Any(attr => IsResharperConditionalAttribute(attr, typeCache));
        }

        private static bool IsResharperConditionalAttribute([NotNull] AttributeData attribute,
            [NotNull] FrameworkTypeCache typeCache)
        {
            if (typeCache.ConditionalAttribute != null)
            {
                if (typeCache.ConditionalAttribute.Equals(attribute.AttributeClass))
                {
                    object ctorValue = attribute.ConstructorArguments.First().Value;
                    return (string)ctorValue == "JETBRAINS_ANNOTATIONS";
                }
            }

            return false;
        }

        // TODO: [vNext] Find a more reliable way after next Roslyn release.
        // See: http://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
        // and: https://github.com/dotnet/roslyn/issues/1891
        [NotNull]
        public static string GetFullMetadataName([NotNull] this INamespaceOrTypeSymbol symbol)
        {
            Guard.NotNull(symbol, nameof(symbol));

            ISymbol current = symbol;
            var textBuilder = new StringBuilder(current.MetadataName);

            ISymbol previous = current;
            current = current.ContainingSymbol;
            while (!IsRootNamespace(current))
            {
                if (current is ITypeSymbol && previous is ITypeSymbol)
                {
                    textBuilder.Insert(0, '+');
                }
                else
                {
                    textBuilder.Insert(0, '.');
                }
                textBuilder.Insert(0, current.MetadataName);
                current = current.ContainingSymbol;
            }

            return textBuilder.ToString();
        }

        private static bool IsRootNamespace([CanBeNull] ISymbol symbol)
        {
            var namespaceSymbol = symbol as INamespaceSymbol;
            return namespaceSymbol != null && namespaceSymbol.IsGlobalNamespace;
        }

        public static bool IsInExternalAssembly([NotNull] this ISymbol symbol)
        {
            Guard.NotNull(symbol, nameof(symbol));

            return !symbol.DeclaringSyntaxReferences.Any();
        }
    }
}
