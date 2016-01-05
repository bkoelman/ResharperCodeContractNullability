using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public class RawSourceCodeBuilder : ITestDataBuilder<ParsedSourceCode>
    {
        [NotNull]
        private string sourceText = string.Empty;

        [NotNull]
        private string expectedText = string.Empty;

        public ParsedSourceCode Build()
        {
            return new ExactSourceCode(sourceText, expectedText, SourceCodeBuilder.DefaultFilename,
                new ExternalAnnotationsBuilder().Build(), SourceCodeBuilder.DefaultReferences, string.Empty);
        }

        [NotNull]
        public RawSourceCodeBuilder Exactly([NotNull] string text, [NotNull] string expected)
        {
            Guard.NotNull(text, nameof(text));
            Guard.NotNull(expected, nameof(expected));

            sourceText = NormalizeLineBreaks(text);
            expectedText = NormalizeLineBreaks(expected);
            return this;
        }

        [NotNull]
        private static string NormalizeLineBreaks([NotNull] string text)
        {
            return text.Replace("\n", "\r\n").Replace("\r\r", "\r");
        }

        [NotNull]
        public static string PublicGlobalNullabilityAttributes => @"

public class CanBeNullAttribute : System.Attribute { }
public class NotNullAttribute : System.Attribute { }
public class ItemCanBeNullAttribute : System.Attribute { }
public class ItemNotNullAttribute : System.Attribute { }
";
    }
}