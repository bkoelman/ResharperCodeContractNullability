using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Test.TestDataBuilders;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynTestFramework;

namespace CodeContractNullability.Test
{
    public sealed class ParsedSourceCode
    {
        [NotNull]
        private readonly FixableDocument document;

        [NotNull]
        public AnalyzerTestContext TestContext { get; }

        [NotNull]
        public ExternalAnnotationsMap ExternalAnnotationsMap { get; }

        [NotNull]
        public string ExpectedText => document.ExpectedText;

        public bool IgnoreWhitespaceDifferences { get; }

        [NotNull]
        private readonly string attributePrefix;

        public ParsedSourceCode([NotNull] string text, [NotNull] string fileName, [NotNull] AnalyzerSettings settings,
            [NotNull] ExternalAnnotationsMap externalAnnotationsMap,
            [NotNull] [ItemNotNull] ImmutableHashSet<MetadataReference> references,
            [ItemNotNull] [NotNull] IList<string> nestedTypes, bool ignoreWhitespaceDifferences)
        {
            Guard.NotNull(text, nameof(text));
            Guard.NotNull(fileName, nameof(fileName));
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNull(externalAnnotationsMap, nameof(externalAnnotationsMap));
            Guard.NotNull(references, nameof(references));
            Guard.NotNull(nestedTypes, nameof(nestedTypes));

            document = new FixableDocument(text);
            ExternalAnnotationsMap = externalAnnotationsMap;
            attributePrefix = ExtractAttributePrefix(nestedTypes);
            IgnoreWhitespaceDifferences = ignoreWhitespaceDifferences;

            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(settings);

            TestContext = new AnalyzerTestContext(document.SourceText, document.SourceSpans, LanguageNames.CSharp, options)
                .WithReferences(references)
                .InFileNamed(fileName);
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
        public string GetExpectedTextForAttribute([NotNull] string attributeName)
        {
            Guard.NotNull(attributeName, nameof(attributeName));

            string annotation = "[" + attributePrefix + attributeName + "]";
            return document.ExpectedText.Replace("NullabilityAttributePlaceholder", annotation);
        }
    }
}
