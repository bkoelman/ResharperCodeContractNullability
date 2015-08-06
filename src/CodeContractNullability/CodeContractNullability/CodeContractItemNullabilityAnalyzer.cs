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
    /// <summary>
    /// The Analyzer entry point, which creates diagnostics for members that need item nullability annotation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CodeContractItemNullabilityAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RINUL";
        private const string Category = "Nullability";

        [NotNull]
        private static readonly DiagnosticDescriptor RuleForField = CreateRuleFor("Field");

        [NotNull]
        private static readonly DiagnosticDescriptor RuleForProperty = CreateRuleFor("Property");

        [NotNull]
        private static readonly DiagnosticDescriptor RuleForMethodReturnValue = CreateRuleFor("Method");

        [NotNull]
        private static readonly DiagnosticDescriptor RuleForParameter = CreateRuleFor("Parameter");

        [NotNull]
        private static DiagnosticDescriptor CreateRuleFor([NotNull] string memberTypePascalCase)
        {
            string title = $"{memberTypePascalCase} is missing item nullability annotation.";
            string messageFormat = $"{memberTypePascalCase} '{{0}}' is missing item nullability annotation.";
            string description =
                $"The item type of this sequence/collection {memberTypePascalCase.ToCamelCase()} is a reference type or nullable type; it should be annotated with [ItemNotNull] or [ItemCanBeNull].";

            return new DiagnosticDescriptor(DiagnosticId, title, messageFormat, Category, DiagnosticSeverity.Warning,
                true, description);
        }

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(RuleForField, RuleForProperty, RuleForMethodReturnValue, RuleForParameter);

        [NotNull]
        public ExtensionPoint<INullabilityAttributeProvider> NullabilityAttributeProvider { get; } =
            new ExtensionPoint<INullabilityAttributeProvider>(() => new CachingNullabilityAttributeProvider());

        [NotNull]
        public ExtensionPoint<ExternalAnnotationsMap> ExternalAnnotationsRegistry { get; } =
            new ExtensionPoint<ExternalAnnotationsMap>(DiskExternalAnnotationsLoader.Create);

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
            var info = new MemberAnalysisInfo(nullSymbols, externalAnnotations, generatedCodeCache);

            context.RegisterSymbolAction(c => AnalyzeField(c, info), SymbolKind.Field);
            context.RegisterSymbolAction(c => AnalyzeProperty(c, info), SymbolKind.Property);
            context.RegisterSymbolAction(c => AnalyzeMethod(c, info), SymbolKind.Method);
            context.RegisterSyntaxNodeAction(c => AnalyzeParameterSyntax(c, info), SyntaxKind.Parameter);
        }

        private void AnalyzeField(SymbolAnalysisContext context, [NotNull] MemberAnalysisInfo info)
        {
            var analyzer = new FieldAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache, true);
            analyzer.Analyze(RuleForField, info.Properties);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context, [NotNull] MemberAnalysisInfo info)
        {
            var analyzer = new PropertyAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache, true);
            analyzer.Analyze(RuleForProperty, info.Properties);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context, [NotNull] MemberAnalysisInfo info)
        {
            var analyzer = new MethodReturnValueAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache,
                true);
            analyzer.Analyze(RuleForMethodReturnValue, info.Properties);
        }

        private void AnalyzeParameterSyntax(SyntaxNodeAnalysisContext context, [NotNull] MemberAnalysisInfo info)
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

        private void AnalyzeParameter(SymbolAnalysisContext context, [NotNull] MemberAnalysisInfo info)
        {
            var analyzer = new ParameterAnalyzer(context, info.ExternalAnnotations, info.GeneratedCodeCache, true);
            analyzer.Analyze(RuleForParameter, info.Properties);
        }

        private sealed class MemberAnalysisInfo
        {
            [NotNull]
            public ImmutableDictionary<string, string> Properties { get; }

            [NotNull]
            public ExternalAnnotationsMap ExternalAnnotations { get; }

            [NotNull]
            public GeneratedCodeDocumentCache GeneratedCodeCache { get; }

            public MemberAnalysisInfo([NotNull] NullabilityAttributeSymbols nullSymbols,
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