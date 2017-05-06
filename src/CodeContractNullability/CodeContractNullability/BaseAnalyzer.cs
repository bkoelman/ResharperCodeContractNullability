using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using CodeContractNullability.ExternalAnnotations;
using CodeContractNullability.NullabilityAttributes;
using CodeContractNullability.Settings;
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
        public const string CreateConfigurationDiagnosticId = "CFGNUL";

        protected const string Category = "Nullability";

        private const string ConfigurationRuleTitle = "Suggest to add nullability configuration file to the project.";

        private const string ConfigurationRuleHelpUrl =
                "https://github.com/bkoelman/ResharperCodeContractNullability/blob/master/doc/reference/CFGNUL_SuggestToAddNullabilityConfigurationFileToProject.md"
            ;

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
        private readonly DiagnosticDescriptor createConfigurationRule;

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ruleForField,
            ruleForProperty, ruleForMethodReturnValue, ruleForParameter, createConfigurationRule);

        [NotNull]
        public ExtensionPoint<INullabilityAttributeProvider> NullabilityAttributeProvider { get; } =
            new ExtensionPoint<INullabilityAttributeProvider>(() => new CachingNullabilityAttributeProvider());

        [NotNull]
        public ExtensionPoint<IExternalAnnotationsResolver> ExternalAnnotationsResolver { get; } =
            new ExtensionPoint<IExternalAnnotationsResolver>(() => new CachingExternalAnnotationsResolver());

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected BaseAnalyzer(bool appliesToItem)
        {
            createConfigurationRule = ConstructCreateConfigurationRule();

            this.appliesToItem = appliesToItem;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            ruleForField = CreateRuleFor("Field");
            ruleForProperty = CreateRuleFor("Property");
            ruleForMethodReturnValue = CreateRuleFor("Method");
            ruleForParameter = CreateRuleFor("Parameter");
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        [NotNull]
        private DiagnosticDescriptor ConstructCreateConfigurationRule()
        {
            string description = null;
            string messageFormat = "Add nullability configuration file to the project.";

            if (IsRunningAsExtension())
            {
                messageFormat += "\n\nIMPORTANT: Additional steps are needed. Expand the arrow on the left for details.";
                description =
                    "Click 'More about CFGNUL' to the right for instructions on how to activate this configuration file.";
            }

            return new DiagnosticDescriptor(CreateConfigurationDiagnosticId, ConfigurationRuleTitle, messageFormat,
                "Configuration", DiagnosticSeverity.Hidden, true, description, ConfigurationRuleHelpUrl);
        }

        private bool IsRunningAsExtension()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyPath = assembly.Location;

            if (!string.IsNullOrEmpty(assemblyPath))
            {
                string assemblyFolder = Path.GetDirectoryName(assemblyPath);
                if (!string.IsNullOrEmpty(assemblyFolder))
                {
                    return File.Exists(Path.Combine(assemblyFolder, "extension.vsixmanifest"));
                }
            }

            return false;
        }

        public override void Initialize([NotNull] AnalysisContext context)
        {
            Guard.NotNull(context, nameof(context));

            context.RegisterCompilationStartAction(StartAnalyzeCompilation);
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

            var nullabilityContext = new AnalysisScope(resolver, generatedCodeCache, typeCache, settings, createConfigurationRule,
                appliesToItem);

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
