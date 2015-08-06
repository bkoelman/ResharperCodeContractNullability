using System.Threading;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.NullabilityAttributes
{
    /// <summary>
    /// Provides cached access to the (Item)NotNullAttribute and (Item)CanBeNullAttribute symbols in a compilation. The cache
    /// is used for hinting, resulting in potential faster lookups over (similar) compilations and solutions.
    /// </summary>
    public sealed class CachingNullabilityAttributeProvider : INullabilityAttributeProvider
    {
        [NotNull]
        private static readonly FreshReference<NullabilityAttributeMetadataNames> LastSeenNames =
            new FreshReference<NullabilityAttributeMetadataNames>(null);

        [NotNull]
        private readonly FreshReference<NullabilityAttributeMetadataNames> names =
            new FreshReference<NullabilityAttributeMetadataNames>(null);

        [NotNull]
        private readonly FreshReference<NullabilityAttributeSymbols> symbols =
            new FreshReference<NullabilityAttributeSymbols>(null);

        public CachingNullabilityAttributeProvider([CanBeNull] NullabilityAttributeMetadataNames names = null)
        {
            this.names.Value = names;
        }

        public NullabilityAttributeSymbols GetSymbols(Compilation compilation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(compilation, nameof(compilation));

            NullabilityAttributeSymbols symbolsSnapshot = symbols.Value;

            NullabilityAttributeMetadataNames previousNames = symbolsSnapshot?.GetMetadataNames() ??
                names.Value ?? LastSeenNames.Value;
            symbolsSnapshot = previousNames?.GetSymbolsOrNull(compilation);

            if (symbolsSnapshot == null)
            {
                var provider = new SimpleNullabilityAttributeProvider();
                symbolsSnapshot = provider.GetSymbols(compilation, cancellationToken);
            }

            if (symbolsSnapshot != null)
            {
                names.Value = symbolsSnapshot.GetMetadataNames();
                LastSeenNames.Value = names.Value;
            }

            symbols.Value = symbolsSnapshot;
            return symbolsSnapshot;
        }
    }
}