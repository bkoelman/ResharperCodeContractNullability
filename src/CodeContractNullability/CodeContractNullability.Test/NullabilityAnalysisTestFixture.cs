using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.Test.TestDataBuilders;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynTestFramework;

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

        private protected void VerifyNullabilityDiagnostic([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            externalAnnotationsResolver = new SimpleExternalAnnotationsResolver(source.ExternalAnnotationsMap);

            AssertDiagnostics(source.TestContext, messages);
        }

        private protected virtual void VerifyNullabilityFix([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));

            externalAnnotationsResolver = new SimpleExternalAnnotationsResolver(source.ExternalAnnotationsMap);

            string fixNotNull = source.GetExpectedTextForAttribute(NotNullAttributeName);
            string fixCanBeNull = source.GetExpectedTextForAttribute(CanBeNullAttributeName);

            string[] expectedCode =
            {
                fixNotNull,
                fixCanBeNull
            };

            var fixContext = new FixProviderTestContext(source.TestContext, expectedCode,
                source.CodeComparisonMode);

            AssertDiagnosticsWithCodeFixes(fixContext, messages);
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
