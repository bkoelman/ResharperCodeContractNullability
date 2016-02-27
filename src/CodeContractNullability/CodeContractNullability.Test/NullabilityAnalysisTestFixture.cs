using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.Test.RoslynTestFramework;
using CodeContractNullability.Test.TestDataBuilders;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.Test
{
    internal abstract class NullabilityAnalysisTestFixture : AnalysisTestFixture
    {
        [NotNull]
        private IExternalAnnotationsResolver externalAnnotationsResolver =
            new SimpleExternalAnnotationsResolver(new ExternalAnnotationsBuilder().Build());

        [NotNull]
        protected abstract string NotNullAttributeName { get; }

        [NotNull]
        protected abstract string CanBeNullAttributeName { get; }

        protected void VerifyNullabilityDiagnostic([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            externalAnnotationsResolver = new SimpleExternalAnnotationsResolver(source.ExternalAnnotationsMap);

            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            AnalyzerTestContext analyzerTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename)
                .WithOptions(options);

            AssertDiagnostics(analyzerTextContext);
        }

        protected void VerifyNullabilityFix([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            string fixNotNull = source.GetExpectedTextForAttribute(NotNullAttributeName);
            string fixCanBeNull = source.GetExpectedTextForAttribute(CanBeNullAttributeName);

            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            AnalyzerTestContext analyzeTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename)
                .WithOptions(options);

            var fixTestContext = new FixProviderTestContext(analyzeTextContext, new[] { fixNotNull, fixCanBeNull },
                source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixTestContext);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            BaseAnalyzer analyzer = CreateNullabilityAnalyzer();
            analyzer.ExternalAnnotationsResolver.Override(externalAnnotationsResolver);
            analyzer.NullabilityAttributeProvider.Override(new SimpleNullabilityAttributeProvider());
            return analyzer;
        }

        [NotNull]
        protected abstract BaseAnalyzer CreateNullabilityAnalyzer();
    }
}