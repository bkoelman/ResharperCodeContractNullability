using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    internal sealed class SymbolAnalyzerFactory
    {
        [NotNull]
        private readonly AnalysisScope scope;

        public SymbolAnalyzerFactory([NotNull] AnalysisScope scope)
        {
            Guard.NotNull(scope, nameof(scope));

            this.scope = scope;
        }

        [NotNull]
        public FieldAnalyzer GetFieldAnalyzer(SymbolAnalysisContext context)
        {
            return new(context, scope);
        }

        [NotNull]
        public PropertyAnalyzer GetPropertyAnalyzer(SymbolAnalysisContext context)
        {
            return new(context, scope);
        }

        [NotNull]
        public MethodReturnValueAnalyzer GetMethodReturnValueAnalyzer(SymbolAnalysisContext context)
        {
            return new(context, scope);
        }

        [NotNull]
        public ParameterAnalyzer GetParameterAnalyzer(SymbolAnalysisContext context)
        {
            return new(context, scope);
        }
    }
}
