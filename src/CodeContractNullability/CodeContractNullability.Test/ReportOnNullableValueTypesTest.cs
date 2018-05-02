using CodeContractNullability.Utilities;
using Microsoft.CodeAnalysis.CodeFixes;
using RoslynTestFramework;

namespace CodeContractNullability.Test
{
    public abstract class ReportOnNullableValueTypesTest : NullabilityTest
    {
        protected override string DiagnosticId => BaseAnalyzer.DisableReportOnNullableValueTypesDiagnosticId;

        protected override string NotNullAttributeName => "ignored";

        protected override CodeFixProvider CreateFixProvider()
        {
            return new DisableReportOnNullableValueTypesCodeFixProvider();
        }

        protected override void VerifyNullabilityFix(ParsedSourceCode source, params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            var fixTestContext = new FixProviderTestContext(source.TestContext, new[] { source.ExpectedText }, false);

            AssertDiagnosticsWithCodeFixes(fixTestContext, messages);
        }
    }
}
