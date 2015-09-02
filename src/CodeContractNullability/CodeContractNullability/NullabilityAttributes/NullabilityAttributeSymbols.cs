using System.Collections.Immutable;
using CodeContractNullability.SymbolAnalysis;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
    /// <summary>
    /// Exposes the found nullability attributes as (source or external) symbols.
    /// </summary>
    public sealed class NullabilityAttributeSymbols
    {
        [NotNull]
        public INamedTypeSymbol NotNull { get; }

        [NotNull]
        public INamedTypeSymbol CanBeNull { get; }

        [NotNull]
        public INamedTypeSymbol ItemNotNull { get; }

        [NotNull]
        public INamedTypeSymbol ItemCanBeNull { get; }

        public NullabilityAttributeSymbols([NotNull] INamedTypeSymbol notNull, [NotNull] INamedTypeSymbol canBeNull,
            [NotNull] INamedTypeSymbol itemNotNull, [NotNull] INamedTypeSymbol itemCanBeNull)
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

        [NotNull]
        public NullabilityAttributeMetadataNames GetMetadataNames()
        {
            return new NullabilityAttributeMetadataNames(NotNull.GetFullMetadataName(), CanBeNull.GetFullMetadataName(),
                ItemNotNull.GetFullMetadataName(), ItemCanBeNull.GetFullMetadataName());
        }

        [NotNull]
        public ImmutableDictionary<string, string> GetMetadataNamesAsProperties()
        {
            return GetMetadataNames().ToImmutableDictionary();
        }
    }
}