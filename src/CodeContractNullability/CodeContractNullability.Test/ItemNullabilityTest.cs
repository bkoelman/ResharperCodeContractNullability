using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability.Test
{
    public abstract class ItemNullabilityTest : NullabilityAnalysisTestFixture
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

        [NotNull]
        protected static string CreateMessageForField([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Field)
                .Named(name)
                .ForItem()
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForProperty([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Property)
                .Named(name)
                .ForItem()
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForMethod([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Method)
                .Named(name)
                .ForItem()
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForParameter([NotNull] string name)
        {
            return new DiagnosticMessageBuilder()
                .OfType(SymbolType.Parameter)
                .Named(name)
                .ForItem()
                .Build();
        }
    }
}
