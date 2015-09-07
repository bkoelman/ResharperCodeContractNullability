using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.Test.RoslynTestFramework;
using CodeContractNullability.Test.TestDataBuilders;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.Test
{
    internal abstract class ItemNullabilityNUnitRoslynTest : AnalysisTestFixture
    {
        [NotNull]
        private ExternalAnnotationsMap externalAnnotationsMap = new ExternalAnnotationsBuilder().Build();

        protected override string DiagnosticId => CodeContractItemNullabilityAnalyzer.DiagnosticId;

        protected void VerifyItemNullabilityDiagnostic([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            externalAnnotationsMap = source.ExternalAnnotationsMap;

            AnalyzerTestContext analyzerTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename);

            AssertDiagnostics(analyzerTextContext);
        }

        protected void VerifyItemNullabilityFix([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            string fixNotNull = source.GetExpectedTextForAttribute("ItemNotNull");
            string fixCanBeNull = source.GetExpectedTextForAttribute("ItemCanBeNull");

            AnalyzerTestContext analyzeTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename);
            var fixTestContext = new FixProviderTestContext(analyzeTextContext, new[] { fixNotNull, fixCanBeNull },
                source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixTestContext);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            var analyzer = new CodeContractItemNullabilityAnalyzer();
            analyzer.ExternalAnnotationsRegistry.Override(externalAnnotationsMap);
            analyzer.NullabilityAttributeProvider.Override(new SimpleNullabilityAttributeProvider());
            return analyzer;
        }

        protected override CodeFixProvider CreateFixProvider()
        {
            return new CodeContractItemNullabilityCodeFixProvider();
        }
    }
}