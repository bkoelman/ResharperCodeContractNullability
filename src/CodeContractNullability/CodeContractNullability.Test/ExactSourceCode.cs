using System.Collections.Immutable;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.Test
{
    public class ExactSourceCode : ParsedSourceCode
    {
        [NotNull]
        private readonly string sourceExpected;

        public ExactSourceCode([NotNull] string text, [NotNull] string sourceExpected, [NotNull] string filename,
            [NotNull] ExternalAnnotationsMap externalAnnotationsMap,
            [NotNull] [ItemNotNull] ImmutableHashSet<MetadataReference> references,
            [NotNull] string codeNamespaceImport)
            : base(text, filename, externalAnnotationsMap, references, codeNamespaceImport, false)
        {
            Guard.NotNull(sourceExpected, nameof(sourceExpected));

            this.sourceExpected = sourceExpected;
        }

        public override string GetExpectedTextFor(string fixText)
        {
            Guard.NotNull(fixText, nameof(fixText));

            return sourceExpected.Replace(FixMarker, fixText);
        }
    }
}