using CodeContractNullability.ExternalAnnotations;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.SymbolAnalysis
{
    /// <summary>
    /// Performs analysis of a field.
    /// </summary>
    public class FieldAnalyzer : BaseSymbolAnalyzer<IFieldSymbol>
    {
        public FieldAnalyzer(SymbolAnalysisContext context, [NotNull] IExternalAnnotationsResolver externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, [NotNull] FrameworkTypeCache typeCache,
            bool appliesToItem)
            : base(context, externalAnnotations, generatedCodeCache, typeCache, appliesToItem)
        {
        }

        protected override ITypeSymbol GetSymbolType()
        {
            return Symbol.Type;
        }

        protected override bool RequiresAnnotation()
        {
            return !Symbol.HasConstantValue;
        }
    }
}