using System.Collections.Immutable;
using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.ExternalAnnotations.Storage;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.SymbolAnalysis;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability
{
    public abstract class BaseAnalyzer : DiagnosticAnalyzer
    {
        protected const string Category = "Nullability";
        private readonly bool appliesToItem;

        [NotNull]
        private readonly DiagnosticDescriptor ruleForField;

        [NotNull]
        private readonly DiagnosticDescriptor ruleForProperty;

        [NotNull]
        private readonly DiagnosticDescriptor ruleForMethodReturnValue;

        [NotNull]
        private readonly DiagnosticDescriptor ruleForParameter;

        [NotNull]
        protected abstract DiagnosticDescriptor CreateRuleFor([NotNull] string memberTypePascalCase);

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(ruleForField, ruleForProperty, ruleForMethodReturnValue, ruleForParameter);

        [NotNull]
        public ExtensionPoint<INullabilityAttributeProvider> NullabilityAttributeProvider { get; } =
            new ExtensionPoint<INullabilityAttributeProvider>(() => new CachingNullabilityAttributeProvider());

        [NotNull]
        public ExtensionPoint<ExternalAnnotationsMap> ExternalAnnotationsRegistry { get; } =
            new ExtensionPoint<ExternalAnnotationsMap>(DiskExternalAnnotationsLoader.Create);

        protected BaseAnalyzer(bool appliesToItem)
        {
            this.appliesToItem = appliesToItem;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            ruleForField = CreateRuleFor("Field");
            ruleForProperty = CreateRuleFor("Property");
            ruleForMethodReturnValue = CreateRuleFor("Method");
            ruleForParameter = CreateRuleFor("Parameter");
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        public override void Initialize([NotNull] AnalysisContext context)
        {
            Guard.NotNull(context, nameof(context));

            context.RegisterCompilationStartAction(StartAnalyzeCompilation);
        }

        private void StartAnalyzeCompilation([NotNull] CompilationStartAnalysisContext context)
        {
            Guard.NotNull(context, nameof(context));

            NullabilityAttributeSymbols nullSymbols =
                NullabilityAttributeProvider.GetCached().GetSymbols(context.Compilation, context.CancellationToken);
            if (nullSymbols == null)
            {
                // Nullability attributes not found; keep silent.
                return;
            }

            ExternalAnnotationsMap externalAnnotations = ExternalAnnotationsRegistry.GetCached();
            var generatedCodeCache = new GeneratedCodeDocumentCache();
            var info = new SymbolAnalysisInfo(nullSymbols, externalAnnotations, generatedCodeCache);

            context.RegisterSymbolAction(c => AnalyzeField(c, info), SymbolKind.Field);
            context.RegisterSymbolAction(c => AnalyzeProperty(c, info), SymbolKind.Property);
            context.RegisterSymbolAction(c => AnalyzeMethod(c, info), SymbolKind.Method);
            context.RegisterSyntaxNodeAction(c => AnalyzeParameterSyntax(c, info), SyntaxKind.Parameter);
        }

        private void AnalyzeField(SymbolAnalysisContext context, [NotNull] SymbolAnalysisInfo info)
        {
            var analyzer = new FieldAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache, appliesToItem);
            analyzer.Analyze(ruleForField, info.Properties);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context, [NotNull] SymbolAnalysisInfo info)
        {
            var analyzer = new PropertyAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache,
                appliesToItem);
            analyzer.Analyze(ruleForProperty, info.Properties);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context, [NotNull] SymbolAnalysisInfo info)
        {
            var analyzer = new MethodReturnValueAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache,
                appliesToItem);
            analyzer.Analyze(ruleForMethodReturnValue, info.Properties);
        }

        private void AnalyzeParameterSyntax(SyntaxNodeAnalysisContext context, [NotNull] SymbolAnalysisInfo info)
        {
            ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            SymbolAnalysisContext symbolContext = SyntaxToSymbolContext(context, symbol);
            AnalyzeParameter(symbolContext, info);
        }

        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext context,
            [NotNull] ISymbol symbol)
        {
            Guard.NotNull(symbol, nameof(symbol));

            return new SymbolAnalysisContext(symbol, context.SemanticModel.Compilation, context.Options,
                context.ReportDiagnostic, x => true, context.CancellationToken);
        }

        private void AnalyzeParameter(SymbolAnalysisContext context, [NotNull] SymbolAnalysisInfo info)
        {
            var analyzer = new ParameterAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache,
                appliesToItem);
            analyzer.Analyze(ruleForParameter, info.Properties);
        }

        private sealed class SymbolAnalysisInfo
        {
            [NotNull]
            public ImmutableDictionary<string, string> Properties { get; }

            [NotNull]
            public ExternalAnnotationsMap ExternalAnnotations { get; }

            [NotNull]
            public GeneratedCodeDocumentCache GeneratedCodeCache { get; }

            public SymbolAnalysisInfo([NotNull] NullabilityAttributeSymbols nullSymbols,
                [NotNull] ExternalAnnotationsMap externalAnnotations,
                [NotNull] GeneratedCodeDocumentCache generatedCodeCache)
            {
                Guard.NotNull(nullSymbols, nameof(nullSymbols));
                Guard.NotNull(externalAnnotations, nameof(externalAnnotations));
                Guard.NotNull(generatedCodeCache, nameof(generatedCodeCache));

                Properties = nullSymbols.GetMetadataNamesAsProperties();
                ExternalAnnotations = externalAnnotations;
                GeneratedCodeCache = generatedCodeCache;
            }
        }
    }
}