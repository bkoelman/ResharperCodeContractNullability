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
            : base(text, filename, externalAnnotationsMap, references, null, codeNamespaceImport, false)
        {
            Guard.NotNull(sourceExpected, nameof(sourceExpected));

            this.sourceExpected = sourceExpected;
        }

        public override string GetExpectedTextForAttribute(string attributeName)
        {
            Guard.NotNull(attributeName, nameof(attributeName));

            return sourceExpected.Replace(FixMarker, "[" + attributeName + "]");
        }
    }
}