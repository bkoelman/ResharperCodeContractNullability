using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
    internal sealed class AnalysisScope
    {
        [NotNull]
        public IExternalAnnotationsResolver ExternalAnnotations { get; }

        [NotNull]
        public GeneratedCodeDocumentCache GeneratedCodeCache { get; }

        [NotNull]
        public FrameworkTypeCache TypeCache { get; }

        [NotNull]
        public AnalyzerSettings Settings { get; }

        [NotNull]
        public DiagnosticDescriptor DisableReportOnNullableValueTypesRule { get; }

        public bool AppliesToItem { get; }

        public AnalysisScope([NotNull] IExternalAnnotationsResolver externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, [NotNull] FrameworkTypeCache typeCache,
            [NotNull] AnalyzerSettings settings, [NotNull] DiagnosticDescriptor disableReportOnNullableValueTypesRule,
            bool appliesToItem)
        {
            Guard.NotNull(externalAnnotations, nameof(externalAnnotations));
            Guard.NotNull(generatedCodeCache, nameof(generatedCodeCache));
            Guard.NotNull(typeCache, nameof(typeCache));
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNull(disableReportOnNullableValueTypesRule, nameof(disableReportOnNullableValueTypesRule));

            ExternalAnnotations = externalAnnotations;
            GeneratedCodeCache = generatedCodeCache;
            TypeCache = typeCache;
            Settings = settings;
            DisableReportOnNullableValueTypesRule = disableReportOnNullableValueTypesRule;
            AppliesToItem = appliesToItem;
        }
    }
}
