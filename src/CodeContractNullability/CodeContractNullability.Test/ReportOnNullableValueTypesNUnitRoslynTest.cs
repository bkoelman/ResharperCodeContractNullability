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
    internal abstract class ReportOnNullableValueTypesNUnitRoslynTest : NullabilityNUnitRoslynTest
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

        protected virtual void VerifyDiagnosticWithFix([NotNull] ParsedSourceCode source)
        {
            Guard.NotNull(source, nameof(source));

            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(source.Settings);

            AnalyzerTestContext analyzeTextContext = new AnalyzerTestContext(source.GetText(), LanguageNames.CSharp)
                .WithReferences(source.References)
                .WithFileName(source.Filename)
                .WithOptions(options);

            var fixTestContext = new FixProviderTestContext(analyzeTextContext, new[] { string.Empty },
                source.ReIndentExpected);

            AssertDiagnosticsWithCodeFixes(fixTestContext);
        }
    }
}