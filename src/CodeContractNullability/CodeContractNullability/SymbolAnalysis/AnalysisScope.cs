using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.Settings;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CodeContractNullability.SymbolAnalysis
{
    public sealed class AnalysisScope
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
        public DiagnosticDescriptor CreateConfigurationRule { get; }

        public bool AppliesToItem { get; }

        public AnalysisScope([NotNull] IExternalAnnotationsResolver externalAnnotations,
            [NotNull] GeneratedCodeDocumentCache generatedCodeCache, [NotNull] FrameworkTypeCache typeCache,
            [NotNull] AnalyzerSettings settings, [NotNull] DiagnosticDescriptor createConfigurationRule, bool appliesToItem)
        {
            Guard.NotNull(externalAnnotations, nameof(externalAnnotations));
            Guard.NotNull(generatedCodeCache, nameof(generatedCodeCache));
            Guard.NotNull(typeCache, nameof(typeCache));
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNull(createConfigurationRule, nameof(createConfigurationRule));

            ExternalAnnotations = externalAnnotations;
            GeneratedCodeCache = generatedCodeCache;
            TypeCache = typeCache;
            Settings = settings;
            CreateConfigurationRule = createConfigurationRule;
            AppliesToItem = appliesToItem;
        }
    }
}
