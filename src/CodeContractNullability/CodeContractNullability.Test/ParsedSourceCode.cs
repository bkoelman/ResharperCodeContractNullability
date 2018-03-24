using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Test.RoslynTestFramework;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.Test
{
    public sealed class ParsedSourceCode
    {
        [NotNull]
        private readonly FixableDocument document;

        [NotNull]
        public string Filename { get; }

        [NotNull]
        public AnalyzerSettings Settings { get; }

        [NotNull]
        public ExternalAnnotationsMap ExternalAnnotationsMap { get; }

        [NotNull]
        [ItemNotNull]
        public ImmutableHashSet<MetadataReference> References { get; }

        public bool IgnoreWhitespaceDifferences { get; }

        [NotNull]
        private readonly string attributePrefix;

        public ParsedSourceCode([NotNull] string text, [NotNull] string filename, [NotNull] AnalyzerSettings settings,
            [NotNull] ExternalAnnotationsMap externalAnnotationsMap,
            [NotNull] [ItemNotNull] ImmutableHashSet<MetadataReference> references,
            [ItemNotNull] [NotNull] IList<string> nestedTypes, bool ignoreWhitespaceDifferences)
        {
            Guard.NotNull(text, nameof(text));
            Guard.NotNull(filename, nameof(filename));
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNull(externalAnnotationsMap, nameof(externalAnnotationsMap));
            Guard.NotNull(references, nameof(references));
            Guard.NotNull(nestedTypes, nameof(nestedTypes));

            document = new FixableDocument(text);
            Filename = filename;
            Settings = settings;
            ExternalAnnotationsMap = externalAnnotationsMap;
            References = references;
            attributePrefix = ExtractAttributePrefix(nestedTypes);
            IgnoreWhitespaceDifferences = ignoreWhitespaceDifferences;
        }

        [NotNull]
        private static string ExtractAttributePrefix([NotNull] [ItemNotNull] IList<string> nestedTypes)
        {
            var attributePrefixBuilder = new StringBuilder();
            foreach (string nestedType in nestedTypes)
            {
                int lastSpaceIndex = nestedType.LastIndexOf(' ');
                string typeName = lastSpaceIndex != -1 ? nestedType.Substring(lastSpaceIndex + 1) : nestedType;

                attributePrefixBuilder.Append(typeName);
                attributePrefixBuilder.Append('.');
            }

            return attributePrefixBuilder.ToString();
        }

        [NotNull]
        public string GetText()
        {
            return document.SourceText;
        }

        [NotNull]
        public string GetExpectedTextForAttribute([NotNull] string attributeName)
        {
            Guard.NotNull(attributeName, nameof(attributeName));

            string annotation = "[" + attributePrefix + attributeName + "]";
            return document.ExpectedText.Replace("NullabilityAttributePlaceholder", annotation);
        }
    }
}
