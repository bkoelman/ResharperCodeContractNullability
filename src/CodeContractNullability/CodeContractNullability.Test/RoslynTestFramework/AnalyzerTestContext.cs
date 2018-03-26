using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability.Test.RoslynTestFramework
{
    public sealed class AnalyzerTestContext
    {
        private const string DefaultFileName = "TestDocument";
        private const string DefaultAssemblyName = "TestProject";
        private const DocumentationMode DefaultDocumentationMode = DocumentationMode.None;
        private const TestValidationMode DefaultTestValidationMode = TestValidationMode.AllowCompileWarnings;

        [NotNull]
        [ItemNotNull]
        private static readonly ImmutableHashSet<MetadataReference> DefaultReferences =
            ImmutableHashSet.Create(new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location)
            });

        [NotNull]
        public string SourceCode { get; }

        [NotNull]
        public IList<TextSpan> SourceSpans { get; }

        [NotNull]
        public string LanguageName { get; }

        [NotNull]
        public string FileName { get; }

        [NotNull]
        public string AssemblyName { get; }

        [NotNull]
        [ItemNotNull]
        public ImmutableHashSet<MetadataReference> References { get; }

        public DocumentationMode DocumentationMode { get; }

        [CanBeNull]
        public int? CompilerWarningLevel { get; }

        public TestValidationMode ValidationMode { get; }

        public DiagnosticsCaptureMode DiagnosticsCaptureMode { get; }

        [NotNull]
        public AnalyzerOptions Options { get; }

        private AnalyzerTestContext([NotNull] string sourceCode, [NotNull] IList<TextSpan> sourceSpans,
            [NotNull] string languageName, [NotNull] string fileName, [NotNull] string assemblyName,
            [NotNull] [ItemNotNull] ImmutableHashSet<MetadataReference> references, DocumentationMode documentationMode,
            [CanBeNull] int? compilerWarningLevel, TestValidationMode validationMode,
            DiagnosticsCaptureMode diagnosticsCaptureMode, [NotNull] AnalyzerOptions options)
        {
            SourceCode = sourceCode;
            SourceSpans = sourceSpans;
            LanguageName = languageName;
            FileName = fileName;
            AssemblyName = assemblyName;
            References = references;
            DocumentationMode = documentationMode;
            CompilerWarningLevel = compilerWarningLevel;
            ValidationMode = validationMode;
            DiagnosticsCaptureMode = diagnosticsCaptureMode;
            Options = options;
        }

        public AnalyzerTestContext([NotNull] string sourceCode, [NotNull] IList<TextSpan> sourceSpans,
            [NotNull] string languageName, [NotNull] AnalyzerOptions options)
            : this(sourceCode, sourceSpans, languageName, DefaultFileName, DefaultAssemblyName, DefaultReferences,
                DefaultDocumentationMode, null, DefaultTestValidationMode, DiagnosticsCaptureMode.RequireInSourceTree, options)
        {
            Guard.NotNull(sourceCode, nameof(sourceCode));
            Guard.NotNull(sourceSpans, nameof(sourceSpans));
            Guard.NotNullNorWhiteSpace(languageName, nameof(languageName));
            Guard.NotNull(options, nameof(options));
        }

        [NotNull]
        public AnalyzerTestContext InFileNamed([NotNull] string fileName)
        {
            Guard.NotNullNorWhiteSpace(fileName, nameof(fileName));

            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, fileName, AssemblyName, References,
                DocumentationMode, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext InAssemblyNamed([NotNull] string assemblyName)
        {
            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, FileName, assemblyName, References,
                DocumentationMode, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext WithReferences([NotNull] [ItemNotNull] IEnumerable<MetadataReference> references)
        {
            Guard.NotNull(references, nameof(references));

            ImmutableList<MetadataReference> referenceList = ImmutableList.CreateRange(references);

            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, FileName, AssemblyName,
                referenceList.ToImmutableHashSet(), DocumentationMode, CompilerWarningLevel, ValidationMode,
                DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext WithDocumentationMode(DocumentationMode mode)
        {
            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, FileName, AssemblyName, References, mode,
                CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext CompileAtWarningLevel(int warningLevel)
        {
            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, warningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext InValidationMode(TestValidationMode validationMode)
        {
            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, CompilerWarningLevel, validationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext AllowingDiagnosticsOutsideSourceTree()
        {
            return new AnalyzerTestContext(SourceCode, SourceSpans, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode.AllowOutsideSourceTree, Options);
        }
    }
}
