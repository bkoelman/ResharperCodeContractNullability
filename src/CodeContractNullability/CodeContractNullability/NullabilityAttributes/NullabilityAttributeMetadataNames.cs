using System.Collections.Immutable;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
    /// <summary>
    /// Holds information about where the nullability attributes are located.
    /// </summary>
    public sealed class NullabilityAttributeMetadataNames
    {
        private const string KeyNotNull = "NotNull";
        private const string KeyCanBeNull = "CanBeNull";
        private const string KeyItemNotNull = "ItemNotNull";
        private const string KeyItemCanBeNull = "ItemCanBeNull";

        [NotNull]
        private string NotNull { get; }

        [NotNull]
        private string CanBeNull { get; }

        [NotNull]
        private string ItemNotNull { get; }

        [NotNull]
        private string ItemCanBeNull { get; }

        public NullabilityAttributeMetadataNames([NotNull] string notNull, [NotNull] string canBeNull,
            [NotNull] string itemNotNull, [NotNull] string itemCanBeNull)
        {
            Guard.NotNull(notNull, nameof(notNull));
            Guard.NotNull(canBeNull, nameof(canBeNull));
            Guard.NotNull(itemNotNull, nameof(itemNotNull));
            Guard.NotNull(itemCanBeNull, nameof(itemCanBeNull));

            NotNull = notNull;
            CanBeNull = canBeNull;
            ItemNotNull = itemNotNull;
            ItemCanBeNull = itemCanBeNull;
        }

        [CanBeNull]
        public NullabilityAttributeSymbols GetSymbolsOrNull([NotNull] Compilation compilation)
        {
            Guard.NotNull(compilation, nameof(compilation));

            INamedTypeSymbol notNullSymbol = GetVisibleAttribute(NotNull, compilation);
            INamedTypeSymbol canBeNullSymbol = GetVisibleAttribute(CanBeNull, compilation);
            INamedTypeSymbol itemNotNullSymbol = GetVisibleAttribute(ItemNotNull, compilation);
            INamedTypeSymbol itemCanBeNullSymbol = GetVisibleAttribute(ItemCanBeNull, compilation);

            return notNullSymbol != null && canBeNullSymbol != null && itemNotNullSymbol != null && itemCanBeNullSymbol != null
                ? new NullabilityAttributeSymbols(notNullSymbol, canBeNullSymbol, itemNotNullSymbol, itemCanBeNullSymbol)
                : null;
        }

        [CanBeNull]
        private INamedTypeSymbol GetVisibleAttribute([NotNull] string fullTypeName, [NotNull] Compilation compilation)
        {
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
            return attributeSymbol != null && IsDefinedInSameAssembly(attributeSymbol, compilation.Assembly)
                ? attributeSymbol
                : null;
        }

        private bool IsDefinedInSameAssembly([NotNull] INamedTypeSymbol type, [NotNull] IAssemblySymbol assembly)
        {
            return type.ContainingAssembly.Equals(assembly);
        }

        [NotNull]
        public ImmutableDictionary<string, string> ToImmutableDictionary()
        {
            return ImmutableDictionary.Create<string, string>().Add(KeyNotNull, NotNull).Add(KeyCanBeNull, CanBeNull)
                .Add(KeyItemNotNull, ItemNotNull).Add(KeyItemCanBeNull, ItemCanBeNull);
        }

        [NotNull]
        public static NullabilityAttributeMetadataNames FromImmutableDictionary(
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            Guard.NotNull(properties, nameof(properties));

            return new NullabilityAttributeMetadataNames(properties[KeyNotNull], properties[KeyCanBeNull],
                properties[KeyItemNotNull], properties[KeyItemCanBeNull]);
        }
    }
}
