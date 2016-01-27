using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    public sealed class SymbolAnalyzerFactory
    {
        [NotNull]
        private readonly IExternalAnnotationsResolver externalAnnotations;

        [NotNull]
        private readonly GeneratedCodeDocumentCache generatedCodeCache;

        [NotNull]
        private readonly FrameworkTypeCache typeCache;

        private readonly bool appliesToItem;

        public SymbolAnalyzerFactory([NotNull] IExternalAnnotationsResolver externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, [NotNull] FrameworkTypeCache typeCache,
            bool appliesToItem)
        {
            Guard.NotNull(externalAnnotations, nameof(externalAnnotations));
            Guard.NotNull(generatedCodeCache, nameof(generatedCodeCache));
            Guard.NotNull(typeCache, nameof(typeCache));

            this.externalAnnotations = externalAnnotations;
            this.generatedCodeCache = generatedCodeCache;
            this.typeCache = typeCache;
            this.appliesToItem = appliesToItem;
        }

        [NotNull]
        public FieldAnalyzer GetFieldAnalyzer(SymbolAnalysisContext context)
        {
            return new FieldAnalyzer(context, externalAnnotations, generatedCodeCache, typeCache, appliesToItem);
        }

        [NotNull]
        public PropertyAnalyzer GetPropertyAnalyzer(SymbolAnalysisContext context)
        {
            return new PropertyAnalyzer(context, externalAnnotations, generatedCodeCache, typeCache, appliesToItem);
        }

        [NotNull]
        public MethodReturnValueAnalyzer GetMethodReturnValueAnalyzer(SymbolAnalysisContext context)
        {
            return new MethodReturnValueAnalyzer(context, externalAnnotations, generatedCodeCache, typeCache,
                appliesToItem);
        }

        [NotNull]
        public ParameterAnalyzer GetParameterAnalyzer(SymbolAnalysisContext context)
        {
            return new ParameterAnalyzer(context, externalAnnotations, generatedCodeCache, typeCache, appliesToItem);
        }
    }
}