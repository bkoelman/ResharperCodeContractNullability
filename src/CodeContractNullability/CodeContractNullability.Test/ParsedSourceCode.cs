using System.Collections.Generic;
using System.Text;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
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

        public ParsedSourceCode([NotNull] string sourceText, [NotNull] AnalyzerTestContext testContext,
            [NotNull] ExternalAnnotationsMap externalAnnotationsMap, [ItemNotNull] [NotNull] IList<string> nestedTypes,
            bool ignoreWhitespaceDifferences)
        {
            Guard.NotNull(sourceText, nameof(sourceText));
            Guard.NotNull(testContext, nameof(testContext));
            Guard.NotNull(externalAnnotationsMap, nameof(externalAnnotationsMap));
            Guard.NotNull(nestedTypes, nameof(nestedTypes));

            document = new FixableDocument(sourceText);
            TestContext = testContext.WithCode(document.SourceText, document.SourceSpans);
            ExternalAnnotationsMap = externalAnnotationsMap;
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
        public string GetExpectedTextForAttribute([NotNull] string attributeName)
        {
            Guard.NotNull(attributeName, nameof(attributeName));

            string annotation = "[" + attributePrefix + attributeName + "]";
            return document.ExpectedText.Replace("NullabilityAttributePlaceholder", annotation);
        }
    }
}
