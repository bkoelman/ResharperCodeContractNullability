using System;
using System.IO;
using System.Text;
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
    internal abstract class NullabilityNUnitRoslynTest : AnalysisTestFixture
    {
        [NotNull]
        private ExternalAnnotationsMap externalAnnotationsMap = new ExternalAnnotationsBuilder().Build();

        protected override string DiagnosticId => CodeContractNullabilityAnalyzer.DiagnosticId;

        protected void VerifyNullabilityDiagnostic([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            externalAnnotationsMap = source.ExternalAnnotationsMap;

            AnalyzerTestContext analyzerTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename);

            AssertDiagnostics(analyzerTextContext);
        }

        protected void VerifyNullabilityFix([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            string fixNotNull = source.GetExpectedTextFor("[NotNull]");
            string fixCanBeNull = source.GetExpectedTextFor("[CanBeNull]");

            AnalyzerTestContext analyzeTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename);
            var fixTestContext = new FixProviderTestContext(analyzeTextContext,
                new[] { fixNotNull, fixCanBeNull }, source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixTestContext);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            var analyzer = new CodeContractNullabilityAnalyzer();
            analyzer.ExternalAnnotationsRegistry.Override(externalAnnotationsMap);
            analyzer.NullabilityAttributeProvider.Override(new SimpleNullabilityAttributeProvider());
            return analyzer;
        }

        protected override CodeFixProvider CreateFixProvider()
        {
            return new CodeContractNullabilityCodeFixProvider();
        }

        [NotNull]
        protected static string RemoveLinesWithAnnotation([NotNull] string sourceText)
        {
            Guard.NotNull(sourceText, nameof(sourceText));

            var textBuilder = new StringBuilder();
            using (var reader = new StringReader(sourceText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.IndexOf(ParsedSourceCode.FixMarker, StringComparison.Ordinal) == -1)
                    {
                        textBuilder.AppendLine(line);
                    }
                }
            }

            return textBuilder.ToString();
        }
    }
}