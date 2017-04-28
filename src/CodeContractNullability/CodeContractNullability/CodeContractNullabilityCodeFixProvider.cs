using System.Collections.Immutable;
using System.Composition;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeContractNullability
{
    /// <summary>
    /// Provides code fixes for diagnostics reported by <see cref="CodeContractNullabilityAnalyzer" />.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeContractNullabilityCodeFixProvider))]
    [Shared]
    public sealed class CodeContractNullabilityCodeFixProvider : BaseCodeFixProvider
    {
        [ItemNotNull]
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CodeContractNullabilityAnalyzer
            .DiagnosticId);

        public CodeContractNullabilityCodeFixProvider()
            : base(false)
        {
        }
    }
}
