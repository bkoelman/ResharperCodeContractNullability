using System;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using RoslynTestFramework;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class ExactSourceCodeBuilder : ITestDataBuilder<ParsedSourceCode>
    {
        [NotNull]
        private string sourceText = string.Empty;

        public ParsedSourceCode Build()
        {
            ExternalAnnotationsMap map = new ExternalAnnotationsBuilder().Build();

            return new ParsedSourceCode(sourceText, SourceCodeBuilder.DefaultTestContext, map, Array.Empty<string>(),
                TextComparisonMode.ExactMatch);
        }

        [NotNull]
        public ExactSourceCodeBuilder Exactly([NotNull] string text)
        {
            Guard.NotNull(text, nameof(text));

            sourceText = NormalizeLineBreaks(text);
            return this;
        }

        [NotNull]
        private static string NormalizeLineBreaks([NotNull] string text)
        {
            return text.Replace("\n", "\r\n").Replace("\r\r", "\r");
        }

        [NotNull]
        public static string PublicGlobalNullabilityAttributes =>
            new NullabilityAttributesBuilder().InGlobalNamespace().Build().SourceText;
    }
}
