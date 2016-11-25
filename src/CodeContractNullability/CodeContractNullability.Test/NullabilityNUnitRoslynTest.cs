using System;
using System.IO;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability.Test
{
    public abstract class NullabilityNUnitRoslynTest : NullabilityAnalysisTestFixture
    {
        protected override string DiagnosticId => CodeContractNullabilityAnalyzer.DiagnosticId;

        protected override string NotNullAttributeName => "NotNull";
        protected override string CanBeNullAttributeName => "CanBeNull";

        protected override BaseAnalyzer CreateNullabilityAnalyzer()
        {
            return new CodeContractNullabilityAnalyzer();
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