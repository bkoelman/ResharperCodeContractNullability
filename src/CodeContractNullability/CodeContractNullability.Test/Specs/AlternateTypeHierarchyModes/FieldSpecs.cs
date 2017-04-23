using CodeContractNullability.Settings;
using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs.AlternateTypeHierarchyModes
{
    /// <summary>
    /// Tests for reporting nullability diagnostics on fields with non-default type hierarchy configurations.
    /// </summary>
    public sealed class FieldSpecs : NullabilityTest
    {
        [Fact]
        public void When_field_in_mode_AtHighestSourceInTypeHierarchy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtHighestSourceInTypeHierarchy))
                .InDefaultClass(@"
                    <annotate/> string [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_in_mode_AtTopInTypeHierarchy_it_must_be_reported_and_fixed()
        {
            // Arrange
            ParsedSourceCode source = new MemberSourceCodeBuilder()
                .WithSettings(new AnalyzerSettingsBuilder()
                    .InTypeHierarchyReportMode(TypeHierarchyReportMode.AtTopInTypeHierarchy))
                .InDefaultClass(@"
                    <annotate/> string [|f|];
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }
    }
}
