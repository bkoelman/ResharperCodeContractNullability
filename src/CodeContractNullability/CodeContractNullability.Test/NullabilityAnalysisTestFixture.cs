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
    public abstract class NullabilityAnalysisTestFixture : AnalysisTestFixture
    {
        [NotNull]
        private IExternalAnnotationsResolver externalAnnotationsResolver =
            new SimpleExternalAnnotationsResolver(new ExternalAnnotationsBuilder().Build());

        [NotNull]
        protected abstract string NotNullAttributeName { get; }

        [NotNull]
        protected abstract string CanBeNullAttributeName { get; }

        protected void VerifyNullabilityDiagnostic([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            AnalyzerTestContext analyzerContext = CreateTestContext(source);

            AssertDiagnostics(analyzerContext, messages);
        }

        protected virtual void VerifyNullabilityFix([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            AnalyzerTestContext analyzerContext = CreateTestContext(source);

            string fixNotNull = source.GetExpectedTextForAttribute(NotNullAttributeName);
            string fixCanBeNull = source.GetExpectedTextForAttribute(CanBeNullAttributeName);

            var fixContext = new FixProviderTestContext(analyzerContext, new[] { fixNotNull, fixCanBeNull },
                source.IgnoreWhitespaceDifferences);

            AssertDiagnosticsWithCodeFixes(fixContext, messages);
        }

        [NotNull]
        protected AnalyzerTestContext CreateTestContext([NotNull] ParsedSourceCode source)
        {
            externalAnnotationsResolver = new SimpleExternalAnnotationsResolver(source.ExternalAnnotationsMap);

            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            return new AnalyzerTestContext(source.SourceText, source.SourceSpans, LanguageNames.CSharp, options)
                .WithReferences(source.References)
                .InFileNamed(source.Filename);
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
