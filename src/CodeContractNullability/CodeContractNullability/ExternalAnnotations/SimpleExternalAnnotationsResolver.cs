using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.ExternalAnnotations
{
    /// <summary>
    /// Provides a simple wrapper for an existing <see cref="ExternalAnnotationsMap" />.
    /// </summary>
    public sealed class SimpleExternalAnnotationsResolver : IExternalAnnotationsResolver
    {
        [NotNull]
        private readonly ExternalAnnotationsMap source;

        public SimpleExternalAnnotationsResolver([NotNull] ExternalAnnotationsMap source)
        {
            Guard.NotNull(source, nameof(source));

            this.source = source;
        }

        public void EnsureScanned()
        {
        }

        public bool HasAnnotationForSymbol(ISymbol symbol, bool appliesToItem, Compilation compilation)
        {
            Guard.NotNull(symbol, nameof(symbol));

            return source.Contains(symbol, appliesToItem);
        }
    }
}