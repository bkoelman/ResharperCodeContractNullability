using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using CodeContractNullability.ExternalAnnotations;
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
    /// Common functionality for all the diagnostics analyzers that are included in this project.
    /// </summary>
    public abstract class BaseAnalyzer : DiagnosticAnalyzer
    {
        public const string DisableReportOnNullableValueTypesDiagnosticId = "XNUL";

        protected const string Category = "Nullability";

        [NotNull]
        [ItemCanBeNull]
        private static readonly Lazy<MethodInfo> LazyEnableConcurrentExecutionMethod =
            new Lazy<MethodInfo>(() => typeof(AnalysisContext).GetMethod("EnableConcurrentExecution"),
                LazyThreadSafetyMode.PublicationOnly);

        [NotNull]
        [ItemCanBeNull]
        private static readonly Lazy<MethodInfo> LazyConfigureGeneratedCodeAnalysisMethod =
            new Lazy<MethodInfo>(() => typeof(AnalysisContext).GetMethod("ConfigureGeneratedCodeAnalysis"),
                LazyThreadSafetyMode.PublicationOnly);

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

        [NotNull]
        private readonly DiagnosticDescriptor disableReportOnNullableValueTypesRule = new DiagnosticDescriptor(
            DisableReportOnNullableValueTypesDiagnosticId, "Suggest to disable reporting on nullable value types.",
            "IMPORTANT: Due to a bug in Visual Studio, additional steps are needed. Expand the arrow to the left of this message for details.",
            "Configuration", DiagnosticSeverity.Hidden, true, @"
At this time, the code fix is not able to fully configure the newly-created ResharperCodeContractNullability.config file 
for use. This is tracked in bug report https://github.com/dotnet/roslyn/issues/4655. In the mean time, users must manually 
perform the following additional steps after applying this code fix.

1. Right click the project in [Solution Explorer] and select [Unload Project]. If you are asked to save changes, click [Yes].
2. Right click the unloaded project in [Solution Explorer] and select [Edit ProjectName.csproj].
3. Locate the following item in the project file:

    <None Include=""ResharperCodeContractNullability.config"" />

4. Change the definition to the following:

    <AdditionalFiles Include=""ResharperCodeContractNullability.config"" />

5. Save and close the project file.
6. Right click the unloaded project in [Solution Explorer] and select [Reload Project].",
            "https://github.com/bkoelman/ResharperCodeContractNullability/blob/master/doc/reference/XNUL_SuggestToDisableReportingOnNullableValueTypes.md");

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(ruleForField, ruleForProperty, ruleForMethodReturnValue, ruleForParameter,
                disableReportOnNullableValueTypesRule);

        [NotNull]
        public ExtensionPoint<INullabilityAttributeProvider> NullabilityAttributeProvider { get; } =
            new ExtensionPoint<INullabilityAttributeProvider>(() => new CachingNullabilityAttributeProvider());

        [NotNull]
        public ExtensionPoint<IExternalAnnotationsResolver> ExternalAnnotationsResolver { get; } =
            new ExtensionPoint<IExternalAnnotationsResolver>(() => new CachingExternalAnnotationsResolver());

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
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

            TryEnableConcurrentExecution(context);
            TrySkipGeneratedCode(context);

            context.RegisterCompilationStartAction(StartAnalyzeCompilation);
        }

        private void TryEnableConcurrentExecution([NotNull] AnalysisContext context)
        {
            MethodInfo method = LazyEnableConcurrentExecutionMethod.Value;
            if (method != null)
            {
                method.Invoke(context, new object[0]);
            }
        }

        private void TrySkipGeneratedCode([NotNull] AnalysisContext context)
        {
            MethodInfo method = LazyConfigureGeneratedCodeAnalysisMethod.Value;
            if (method != null)
            {
                method.Invoke(context, new object[] { 0 });
            }
        }

        private void StartAnalyzeCompilation([NotNull] CompilationStartAnalysisContext context)
        {
            Guard.NotNull(context, nameof(context));

            AnalyzerSettings settings = SettingsProvider.LoadSettings(context.Options, context.CancellationToken);

            NullabilityAttributeSymbols nullSymbols = NullabilityAttributeProvider.GetCached()
                .GetSymbols(context.Compilation, context.CancellationToken);
            if (nullSymbols == null)
            {
                // Nullability attributes not found; keep silent.
                return;
            }

            IExternalAnnotationsResolver resolver = ExternalAnnotationsResolver.GetCached();
            resolver.EnsureScanned();

            var generatedCodeCache = new GeneratedCodeDocumentCache();
            var typeCache = new FrameworkTypeCache(context.Compilation);

            var nullabilityContext = new AnalysisScope(resolver, generatedCodeCache, typeCache, settings,
                disableReportOnNullableValueTypesRule, appliesToItem);

            var factory = new SymbolAnalyzerFactory(nullabilityContext);

            ImmutableDictionary<string, string> properties = nullSymbols.GetMetadataNamesAsProperties();

            context.RegisterSymbolAction(c => AnalyzeField(c, factory, properties), SymbolKind.Field);
            context.RegisterSymbolAction(c => AnalyzeProperty(c, factory, properties), SymbolKind.Property);
            context.RegisterSymbolAction(c => AnalyzeMethod(c, factory, properties), SymbolKind.Method);
            context.RegisterSyntaxNodeAction(c => AnalyzeParameter(SyntaxToSymbolContext(c), factory, properties),
                SyntaxKind.Parameter);
        }

        private void AnalyzeField(SymbolAnalysisContext context, [NotNull] SymbolAnalyzerFactory factory,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            FieldAnalyzer analyzer = factory.GetFieldAnalyzer(context);
            analyzer.Analyze(ruleForField, properties);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context, [NotNull] SymbolAnalyzerFactory factory,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            PropertyAnalyzer analyzer = factory.GetPropertyAnalyzer(context);
            analyzer.Analyze(ruleForProperty, properties);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context, [NotNull] SymbolAnalyzerFactory factory,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            MethodReturnValueAnalyzer analyzer = factory.GetMethodReturnValueAnalyzer(context);
            analyzer.Analyze(ruleForMethodReturnValue, properties);
        }

        private void AnalyzeParameter(SymbolAnalysisContext context, [NotNull] SymbolAnalyzerFactory factory,
            [NotNull] ImmutableDictionary<string, string> properties)
        {
            // Bug workaround for https://github.com/dotnet/roslyn/issues/16209
            if (context.Symbol != null)
            {
                ParameterAnalyzer analyzer = factory.GetParameterAnalyzer(context);
                analyzer.Analyze(ruleForParameter, properties);
            }
        }

        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext syntaxContext)
        {
            ISymbol symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
            return SyntaxToSymbolContext(syntaxContext, symbol);
        }

        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext context, [CanBeNull] ISymbol symbol)
        {
            return new SymbolAnalysisContext(symbol, context.SemanticModel.Compilation, context.Options, context.ReportDiagnostic,
                x => true, context.CancellationToken);
        }
    }
}
