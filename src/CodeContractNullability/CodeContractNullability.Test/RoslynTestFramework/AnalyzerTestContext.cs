﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeContractNullability.Test.RoslynTestFramework
{
    public sealed class AnalyzerTestContext
    {
        private const string DefaultFileName = "TestDocument";
        private const string DefaultAssemblyName = "TestProject";
        private const DocumentationMode DefaultDocumentationMode = DocumentationMode.None;
        private const OperationFeature DefaultOperationFeature = OperationFeature.Disabled;
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
        public string MarkupCode { get; }

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

        public OperationFeature OperationFeature { get; }

        [CanBeNull]
        public int? CompilerWarningLevel { get; }

        public TestValidationMode ValidationMode { get; }

        public DiagnosticsCaptureMode DiagnosticsCaptureMode { get; }

        [NotNull]
        public AnalyzerOptions Options { get; }

        private AnalyzerTestContext([NotNull] string markupCode, [NotNull] string languageName,
            [NotNull] string fileName, [NotNull] string assemblyName,
            [NotNull] [ItemNotNull] ImmutableHashSet<MetadataReference> references, DocumentationMode documentationMode,
            OperationFeature operationFeature, [CanBeNull] int? compilerWarningLevel, TestValidationMode validationMode,
            DiagnosticsCaptureMode diagnosticsCaptureMode, [NotNull] AnalyzerOptions options)
        {
            MarkupCode = markupCode;
            LanguageName = languageName;
            FileName = fileName;
            AssemblyName = assemblyName;
            References = references;
            DocumentationMode = documentationMode;
            OperationFeature = operationFeature;
            CompilerWarningLevel = compilerWarningLevel;
            ValidationMode = validationMode;
            DiagnosticsCaptureMode = diagnosticsCaptureMode;
            Options = options;
        }

        public AnalyzerTestContext([NotNull] string markupCode, [NotNull] string languageName,
            [NotNull] AnalyzerOptions options)
            : this(
                markupCode, languageName, DefaultFileName, DefaultAssemblyName, DefaultReferences,
                DefaultDocumentationMode, DefaultOperationFeature, null, DefaultTestValidationMode,
                DiagnosticsCaptureMode.RequireInSourceTree, options)
        {
            Guard.NotNull(markupCode, nameof(markupCode));
            Guard.NotNullNorWhiteSpace(languageName, nameof(languageName));
            Guard.NotNull(options, nameof(options));
        }

        [NotNull]
        public AnalyzerTestContext WithMarkupCode([NotNull] string markupCode)
        {
            Guard.NotNull(markupCode, nameof(markupCode));

            return new AnalyzerTestContext(markupCode, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, OperationFeature, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode,
                Options);
        }

        [NotNull]
        public AnalyzerTestContext InFileNamed([NotNull] string fileName)
        {
            Guard.NotNullNorWhiteSpace(fileName, nameof(fileName));

            return new AnalyzerTestContext(MarkupCode, LanguageName, fileName, AssemblyName, References,
                DocumentationMode, OperationFeature, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode,
                Options);
        }

        [NotNull]
        public AnalyzerTestContext InAssemblyNamed([NotNull] string assemblyName)
        {
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, assemblyName, References,
                DocumentationMode, OperationFeature, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode,
                Options);
        }

        [NotNull]
        public AnalyzerTestContext WithReferences([NotNull] [ItemNotNull] IEnumerable<MetadataReference> references)
        {
            Guard.NotNull(references, nameof(references));

            ImmutableList<MetadataReference> referenceList = ImmutableList.CreateRange(references);

            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, AssemblyName,
                referenceList.ToImmutableHashSet(), DocumentationMode, OperationFeature, CompilerWarningLevel,
                ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext WithDocumentationMode(DocumentationMode mode)
        {
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, AssemblyName, References, mode,
                OperationFeature, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext WithOperationFeature(OperationFeature feature)
        {
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, feature, CompilerWarningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext CompileAtWarningLevel(int warningLevel)
        {
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, OperationFeature, warningLevel, ValidationMode, DiagnosticsCaptureMode, Options);
        }

        [NotNull]
        public AnalyzerTestContext InValidationMode(TestValidationMode validationMode)
        {
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, OperationFeature, CompilerWarningLevel, validationMode, DiagnosticsCaptureMode,
                Options);
        }

        [NotNull]
        public AnalyzerTestContext AllowingDiagnosticsOutsideSourceTree()
        {
            return new AnalyzerTestContext(MarkupCode, LanguageName, FileName, AssemblyName, References,
                DocumentationMode, OperationFeature, CompilerWarningLevel, ValidationMode,
                DiagnosticsCaptureMode.AllowOutsideSourceTree, Options);
        }
    }
}
