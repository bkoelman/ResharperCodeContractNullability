using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability.Test
{
    internal abstract class ItemNullabilityNUnitRoslynTest : NullabilityAnalysisTestFixture
    {
        protected override string DiagnosticId => CodeContractItemNullabilityAnalyzer.DiagnosticId;

        protected override string NotNullAttributeName => "ItemNotNull";
        protected override string CanBeNullAttributeName => "ItemCanBeNull";

        protected override BaseAnalyzer CreateNullabilityAnalyzer()
        {
            return new CodeContractItemNullabilityAnalyzer();
        }

        protected override CodeFixProvider CreateFixProvider()
        {
            return new CodeContractItemNullabilityCodeFixProvider();
        }
    }
}