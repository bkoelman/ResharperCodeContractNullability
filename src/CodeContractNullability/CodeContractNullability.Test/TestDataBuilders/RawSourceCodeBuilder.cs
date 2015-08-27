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

            sourceText = text;
            expectedText = expected;
            return this;
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