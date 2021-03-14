using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.Utilities
{
    internal static class AnalysisContextExtensions
    {
        public static SymbolAnalysisContext ToSymbolContext(this SyntaxNodeAnalysisContext syntaxContext)
        {
            ISymbol symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
            return SyntaxToSymbolContext(syntaxContext, symbol);
        }

        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext context, [CanBeNull] ISymbol symbol)
        {
            return new(symbol, context.SemanticModel.Compilation, context.Options, context.ReportDiagnostic, x => true, context.CancellationToken);
        }
    }
}
