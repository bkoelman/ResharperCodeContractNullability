using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    [TestFixture]
    internal class ReportOnNullableValueTypesSpecs : ReportOnNullableValueTypesNUnitRoslynTest
    {
        [Test]
        public void When_field_type_is_nullable_it_must_be_reported_and_disabled()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .InDefaultClass(@"
                    int? [|f|];
                ")
                .Build();

            // Act and assert
            VerifyDiagnosticWithFix(source);
        }

        [Test]
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
            VerifyDiagnosticWithFix(source);
        }
    }
}