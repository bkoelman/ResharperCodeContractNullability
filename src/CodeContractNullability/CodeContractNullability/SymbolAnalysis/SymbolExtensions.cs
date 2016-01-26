using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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

        public static bool TypeCanContainNull([NotNull] this ITypeSymbol typeSymbol)
        {
            Guard.NotNull(typeSymbol, nameof(typeSymbol));

            bool isVoidType = typeSymbol.SpecialType == SpecialType.System_Void;
            return !isVoidType && (IsSystemNullableType(typeSymbol) || !typeSymbol.IsValueType);
        }

        private static bool IsSystemNullableType([NotNull] ITypeSymbol typeSymbol)
        {
            return typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        [CanBeNull]
        public static ITypeSymbol TryGetItemTypeForSequenceOrCollection([NotNull] this ITypeSymbol typeSymbol,
            [NotNull] Compilation compilation)
        {
            Guard.NotNull(typeSymbol, nameof(typeSymbol));
            Guard.NotNull(compilation, nameof(compilation));

            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

            INamedTypeSymbol genericSequenceType = compilation.GetTypeByMetadataName(typeof (IEnumerable<>).FullName);
            if (genericSequenceType != null)
            {
                foreach (INamedTypeSymbol type in typeSymbol.AllInterfaces.PrependIfNotNull(namedTypeSymbol))
                {
                    if (genericSequenceType.Equals(type.ConstructedFrom))
                    {
                        return type.TypeArguments.Single();
                    }
                }
            }

            INamedTypeSymbol nonGenericSequenceType = compilation.GetTypeByMetadataName(typeof (IEnumerable).FullName);
            if (nonGenericSequenceType != null)
            {
                if (
                    typeSymbol.AllInterfaces.PrependIfNotNull(namedTypeSymbol)
                        .Any(type => nonGenericSequenceType.Equals(type)))
                {
                    INamedTypeSymbol stringType = compilation.GetTypeByMetadataName(typeof (string).FullName);
                    if (!typeSymbol.Equals(stringType))
                    {
                        return compilation.GetTypeByMetadataName(typeof (object).FullName);
                    }
                }
            }

            return null;
        }

        [CanBeNull]
        public static ITypeSymbol TryGetItemTypeForLazyOrGenericTask([NotNull] this ITypeSymbol typeSymbol,
            [NotNull] Compilation compilation)
        {
            Guard.NotNull(typeSymbol, nameof(typeSymbol));
            Guard.NotNull(compilation, nameof(compilation));

            INamedTypeSymbol lazyType = compilation.GetTypeByMetadataName(typeof (Lazy<>).FullName);
            INamedTypeSymbol taskType = compilation.GetTypeByMetadataName(typeof (Task<>).FullName);

            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
            if (namedTypeSymbol?.ConstructedFrom != null)
            {
                bool isMatch = namedTypeSymbol.ConstructedFrom.Equals(lazyType) ||
                    namedTypeSymbol.ConstructedFrom.Equals(taskType);

                if (isMatch)
                {
                    return namedTypeSymbol.TypeArguments.Single();
                }
            }

            return null;
        }

        public static bool HasCompilerGeneratedAnnotation([NotNull] this ISymbol memberSymbol,
            [NotNull] Compilation compilation)
        {
            Guard.NotNull(memberSymbol, nameof(memberSymbol));
            Guard.NotNull(compilation, nameof(compilation));

            INamedTypeSymbol compilerGeneratedAttributeType =
                compilation.GetTypeByMetadataName(typeof (CompilerGeneratedAttribute).FullName);

            if (compilerGeneratedAttributeType != null)
            {
                ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();
                return attributes.Any(attr => compilerGeneratedAttributeType.Equals(attr.AttributeClass));
            }

            return false;
        }

        public static bool HasDebuggerNonUserCodeAnnotation([NotNull] this ISymbol memberSymbol,
            [NotNull] Compilation compilation)
        {
            Guard.NotNull(memberSymbol, nameof(memberSymbol));
            Guard.NotNull(compilation, nameof(compilation));

            INamedTypeSymbol debuggerNonUserCodeAttributeType =
                compilation.GetTypeByMetadataName(typeof (DebuggerNonUserCodeAttribute).FullName);

            if (debuggerNonUserCodeAttributeType != null)
            {
                ImmutableArray<AttributeData> attributes = memberSymbol.GetAttributes();
                return attributes.Any(attr => debuggerNonUserCodeAttributeType.Equals(attr.AttributeClass));
            }

            return false;
        }

        public static bool HasResharperConditionalAnnotation([NotNull] this ISymbol symbol,
            [NotNull] Compilation compilation)
        {
            Guard.NotNull(symbol, nameof(symbol));
            Guard.NotNull(compilation, nameof(compilation));

            ImmutableArray<AttributeData> attributes = symbol.ContainingType.GetAttributes();
            return attributes.Any(attr => IsResharperConditionalAttribute(attr, compilation));
        }

        private static bool IsResharperConditionalAttribute([NotNull] AttributeData attribute,
            [NotNull] Compilation compilation)
        {
            INamedTypeSymbol conditionalAttributeType =
                compilation.GetTypeByMetadataName(typeof (ConditionalAttribute).FullName);

            if (conditionalAttributeType != null)
            {
                if (conditionalAttributeType.Equals(attribute.AttributeClass))
                {
                    object ctorValue = attribute.ConstructorArguments.First().Value;
                    return (string) ctorValue == "JETBRAINS_ANNOTATIONS";
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
            return symbol is INamespaceSymbol && ((INamespaceSymbol) symbol).IsGlobalNamespace;
        }
    }
}