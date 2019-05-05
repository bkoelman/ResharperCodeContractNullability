using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.Test.TestDataBuilders;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynTestFramework;

namespace CodeContractNullability.Test
{
    public abstract class ConversionNullabilityTest : AnalysisTestFixture
    {
        protected override string DiagnosticId => NullableReferenceTypeConversionAnalyzer.DiagnosticId;

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            var analyzer = new NullableReferenceTypeConversionAnalyzer();
            analyzer.NullabilityAttributeProvider.Override(new SimpleNullabilityAttributeProvider());
            return analyzer;
        }

        protected override CodeFixProvider CreateFixProvider()
        {
            return new NullableReferenceTypeConversionCodeFixProvider();
        }

        private protected void VerifyDiagnostics([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            AssertDiagnostics(source.TestContext, messages);
        }

        private protected void VerifyFix([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            string[] expectedCode =
            {
                source.ExpectedText
            };

            var fixContext = new FixProviderTestContext(source.TestContext, expectedCode, source.CodeComparisonMode);

            AssertDiagnosticsWithCodeFixes(fixContext, messages);
        }

        [NotNull]
        protected static string CreateMessageForField([NotNull] string name)
        {
            return new ConversionDiagnosticMessageBuilder()
                .OfType(SymbolType.Field)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForProperty([NotNull] string name)
        {
            return new ConversionDiagnosticMessageBuilder()
                .OfType(SymbolType.Property)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForMethod([NotNull] string name)
        {
            return new ConversionDiagnosticMessageBuilder()
                .OfType(SymbolType.Method)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForDelegate([NotNull] string name)
        {
            return new ConversionDiagnosticMessageBuilder()
                .OfType(SymbolType.Delegate)
                .Named(name)
                .Build();
        }

        [NotNull]
        protected static string CreateMessageForParameter([NotNull] string name)
        {
            return new ConversionDiagnosticMessageBuilder()
                .OfType(SymbolType.Parameter)
                .Named(name)
                .Build();
        }
    }
}
