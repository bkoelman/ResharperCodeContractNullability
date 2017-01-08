using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for disabling reporting on nullable types, which is configurable.
    /// </summary>
    public sealed class ReportOnNullableValueTypesSpecs : ReportOnNullableValueTypesTest
    {
        [Fact]
        public void When_field_type_is_nullable_it_must_be_reported_and_disabled()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    int? [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source,
                "IMPORTANT: Due to a bug in Visual Studio, additional steps are needed. Expand the arrow to the left of this message for details.");
        }

        [Fact]
        public void When_field_type_is_nullable_but_analysis_is_disabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .DisableReportOnNullableValueTypes)
                .InDefaultClass(@"
                    int? f;
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }
    }
}
