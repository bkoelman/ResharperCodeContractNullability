using System;
using CodeContractNullability.Test.RoslynTestFramework;
using CodeContractNullability.Test.TestDataBuilders;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.Test
{
    public abstract class ReportOnNullableValueTypesNUnitRoslynTest : NullabilityNUnitRoslynTest
    {
        protected override string DiagnosticId => BaseAnalyzer.DisableReportOnNullableValueTypesDiagnosticId;

        protected override string NotNullAttributeName => "ignored";

        protected override CodeFixProvider CreateFixProvider()
        {
            return new DisableReportOnNullableValueTypesCodeFixProvider();
        }

        protected override void VerifyNullabilityFix(ParsedSourceCode source)
        {
            throw new NotSupportedException($"Use {nameof(VerifyDiagnosticWithFix)} from tests.");
        }

        protected void VerifyDiagnosticWithFix([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            AnalyzerTestContext analyzeContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp, options)
                .WithReferences(source.References)
                .WithFileName(source.Filename);

            var fixTestContext = new FixProviderTestContext(analyzeContext, new[] { string.Empty },
                source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixTestContext);
        }
    }
}
