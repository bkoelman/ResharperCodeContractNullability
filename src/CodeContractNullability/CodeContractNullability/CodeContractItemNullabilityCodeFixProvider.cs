using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability
{
    /// <summary>
    /// The FixProvider entry point, which generates fixes ("annotate this with ...") for diagnostics.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeContractItemNullabilityCodeFixProvider))]
    [Shared]
    public class CodeContractItemNullabilityCodeFixProvider : CodeFixProvider
    {
        [ItemNotNull]
        public override sealed ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CodeContractItemNullabilityAnalyzer.DiagnosticId);

        [NotNull]
        public override sealed FixAllProvider GetFixAllProvider()
        {
            // Note: this may annotate too much. For instance, when an interface is annotated, its implementation should not.
            // But because at the time of analysis, both are not annotated, a diagnostic is created for both.
            return WellKnownFixAllProviders.BatchFixer;
        }

        [NotNull]
        public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var provider = new MemberFixProvider();
            await provider.ProvideFixes(context, true).ConfigureAwait(false);
        }
    }
}