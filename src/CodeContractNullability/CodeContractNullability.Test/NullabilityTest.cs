using CodeContractNullability.Test.TestDataBuilders;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability.Test
{
    public abstract class NullabilityTest : NullabilityAnalysisTestFixture
    {
        protected override string DiagnosticId => CodeContractNullabilityAnalyzer.DiagnosticId;

        protected override string NotNullAttributeName => "NotNull";
        protected override string CanBeNullAttributeName => "CanBeNull";

        protected override BaseAnalyzer CreateNullabilityAnalyzer()
        {
            return new CodeContractNullabilityAnalyzer();
        }

        protected override CodeFixProvider CreateFixProvider()
        {
            return new CodeContractNullabilityCodeFixProvider();
        }

        [NotNull]
        protected static string CreateMessageForField([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Field)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForProperty([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Property)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForMethod([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Method)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForParameter([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Parameter)
                .Named(name)
                .Build();
        }
    }
}
