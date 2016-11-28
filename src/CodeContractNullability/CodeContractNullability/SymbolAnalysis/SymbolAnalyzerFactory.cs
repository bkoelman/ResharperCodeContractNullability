using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    public sealed class SymbolAnalyzerFactory
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
            return new FieldAnalyzer(context, scope);
        }

        [NotNull]
        public PropertyAnalyzer GetPropertyAnalyzer(SymbolAnalysisContext context)
        {
            return new PropertyAnalyzer(context, scope);
        }

        [NotNull]
        public MethodReturnValueAnalyzer GetMethodReturnValueAnalyzer(SymbolAnalysisContext context)
        {
            return new MethodReturnValueAnalyzer(context, scope);
        }

        [NotNull]
        public ParameterAnalyzer GetParameterAnalyzer(SymbolAnalysisContext context)
        {
            return new ParameterAnalyzer(context, scope);
        }
    }
}
