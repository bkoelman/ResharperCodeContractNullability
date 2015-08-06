using System.Collections.Immutable;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
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

            INamedTypeSymbol notNullSymbol = compilation.GetTypeByMetadataName(NotNull);
            INamedTypeSymbol canBeNullSymbol = compilation.GetTypeByMetadataName(CanBeNull);
            INamedTypeSymbol itemNotNullSymbol = compilation.GetTypeByMetadataName(ItemNotNull);
            INamedTypeSymbol itemCanBeNullSymbol = compilation.GetTypeByMetadataName(ItemCanBeNull);

            return notNullSymbol != null && canBeNullSymbol != null && itemNotNullSymbol != null &&
                itemCanBeNullSymbol != null
                ? new NullabilityAttributeSymbols(notNullSymbol, canBeNullSymbol, itemNotNullSymbol, itemCanBeNullSymbol)
                : null;
        }

        [NotNull]
        public ImmutableDictionary<string, string> ToImmutableDictionary()
        {
            return
                ImmutableDictionary.Create<string, string>()
                    .Add(KeyNotNull, NotNull)
                    .Add(KeyCanBeNull, CanBeNull)
                    .Add(KeyItemNotNull, ItemNotNull)
                    .Add(KeyItemCanBeNull, ItemCanBeNull);
        }

        [NotNull]
        public static NullabilityAttributeMetadataNames FromImmutableDictionary(
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            Guard.NotNull(properties, nameof(properties));
            Guard.HasCount(properties, nameof(properties), 4);

            return new NullabilityAttributeMetadataNames(properties[KeyNotNull], properties[KeyCanBeNull],
                properties[KeyItemNotNull], properties[KeyItemCanBeNull]);
        }
    }
}