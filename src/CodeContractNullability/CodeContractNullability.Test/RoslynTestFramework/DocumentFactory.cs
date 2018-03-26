using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.VisualBasic;

namespace CodeContractNullability.Test.RoslynTestFramework
{
    /// <summary />
    internal sealed class DocumentFactory
    {
        [NotNull]
        private static readonly CSharpCompilationOptions DefaultCSharpCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        [NotNull]
        private static readonly VisualBasicCompilationOptions DefaultBasicCompilationOptions =
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        [NotNull]
        private static readonly CSharpParseOptions DefaultCSharpParseOptions = new CSharpParseOptions();

        [NotNull]
        private static readonly VisualBasicParseOptions DefaultBasicParseOptions = new VisualBasicParseOptions();

        [NotNull]
        public string FormatSourceCode([NotNull] string sourceCode, [NotNull] AnalyzerTestContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(sourceCode, nameof(sourceCode));

            Document document = ToDocument(sourceCode, context);
            return FormatDocument(document);
        }

        [NotNull]
        public static Document ToDocument([NotNull] string code, [NotNull] AnalyzerTestContext context)
        {
            ParseOptions parseOptions = GetParseOptions(context.DocumentationMode, context.LanguageName);
            CompilationOptions compilationOptions = GetCompilationOptions(context.CompilerWarningLevel, context.LanguageName);

            Document document = new AdhocWorkspace()
                .AddProject(context.AssemblyName, context.LanguageName)
                .WithParseOptions(parseOptions)
                .WithCompilationOptions(compilationOptions)
                .AddMetadataReferences(context.References)
                .AddDocument(context.FileName, code);

            return document;
        }

        [NotNull]
        private static ParseOptions GetParseOptions(DocumentationMode documentationMode, [NotNull] string languageName)
        {
            return languageName == LanguageNames.VisualBasic
                ? (ParseOptions)DefaultBasicParseOptions.WithDocumentationMode(documentationMode)
                : DefaultCSharpParseOptions.WithDocumentationMode(documentationMode);
        }

        [NotNull]
        private static CompilationOptions GetCompilationOptions([CanBeNull] int? compilerWarningLevel,
            [NotNull] string languageName)
        {
            if (languageName == LanguageNames.VisualBasic)
            {
                return DefaultBasicCompilationOptions;
            }

            return compilerWarningLevel != null
                ? DefaultCSharpCompilationOptions.WithWarningLevel(compilerWarningLevel.Value)
                : DefaultCSharpCompilationOptions;
        }

        [NotNull]
        public string FormatDocument([NotNull] Document document)
        {
            Guard.NotNull(document, nameof(document));

            SyntaxNode syntaxRoot = document.GetSyntaxRootAsync().Result;

            SyntaxNode formattedSyntaxRoot = Formatter.Format(syntaxRoot, document.Project.Solution.Workspace);
            return formattedSyntaxRoot.ToFullString();
        }
    }
}
