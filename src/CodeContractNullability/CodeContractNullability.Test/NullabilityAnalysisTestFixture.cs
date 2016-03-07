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

            string text = source.GetText();
            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            AnalyzerTestContext analyzerContext = new AnalyzerTestContext(text, LanguageNames.CSharp, options)
                .WithReferences(source.References)
                .WithFileName(source.Filename);

            AssertDiagnostics(analyzerContext);
        }

        protected virtual void VerifyNullabilityFix([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            string fixNotNull = source.GetExpectedTextForAttribute(NotNullAttributeName);
            string fixCanBeNull = source.GetExpectedTextForAttribute(CanBeNullAttributeName);

            string text = source.GetText();
            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            AnalyzerTestContext analyzerContext = new AnalyzerTestContext(text, LanguageNames.CSharp, options)
                .WithReferences(source.References)
                .WithFileName(source.Filename);

            var fixContext = new FixProviderTestContext(analyzerContext, new[] { fixNotNull, fixCanBeNull },
                source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixContext);
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