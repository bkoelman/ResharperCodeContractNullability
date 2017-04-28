using CodeContractNullability.Test.RoslynTestFramework;
using CodeContractNullability.Utilities;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability.Test
{
    public abstract class ReportOnNullableValueTypesTest : NullabilityTest
    {
        protected override string DiagnosticId => BaseAnalyzer.CreateConfigurationDiagnosticId;

        protected override string NotNullAttributeName => "ignored";

        protected override CodeFixProvider CreateFixProvider()
        {
            return new CreateConfigurationCodeFixProvider();
        }

        protected override void VerifyNullabilityFix(ParsedSourceCode source, params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            AnalyzerTestContext analyzerContext = CreateTestContext(source);

            var fixTestContext = new FixProviderTestContext(analyzerContext, new[] { string.Empty }, source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixTestContext, messages);
        }
    }
}
