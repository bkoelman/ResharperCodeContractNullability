using System.Collections.Immutable;
using System.Composition;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability
{
    /// <summary>
    /// Provides code fixes for diagnostics reported by <see cref="CodeContractItemNullabilityAnalyzer" />.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeContractItemNullabilityCodeFixProvider))]
    [Shared]
    public sealed class CodeContractItemNullabilityCodeFixProvider : BaseCodeFixProvider
    {
        [ItemNotNull]
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(CodeContractItemNullabilityAnalyzer.DiagnosticId);

        public CodeContractItemNullabilityCodeFixProvider()
            : base(true)
        {
        }
    }
}
