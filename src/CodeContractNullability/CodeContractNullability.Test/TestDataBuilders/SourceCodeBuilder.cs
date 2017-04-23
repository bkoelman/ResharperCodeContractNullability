using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeContractNullability.Utilities;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public abstract class SourceCodeBuilder : ITestDataBuilder<ParsedSourceCode>
    {
        [NotNull]
        private AnalyzerSettings settings = new AnalyzerSettingsBuilder().Build();

        [NotNull]
        [ItemNotNull]
        private readonly HashSet<string> namespaceImports = new HashSet<string> { "System" };

        [CanBeNull]
        private string headerText;

        [NotNull]
        private string sourceFilename = DefaultFilename;

        [NotNull]
        private ExternalAnnotationsBuilder externalAnnotationsBuilder = new ExternalAnnotationsBuilder();

        [NotNull]
        [ItemNotNull]
        private readonly HashSet<MetadataReference> references = new HashSet<MetadataReference>(DefaultReferences);

        [NotNull]
        private string codeNamespaceImportExpected = string.Empty;

        [CanBeNull]
        private NullabilityAttributesDefinition nullabilityAttributes =
            new NullabilityAttributesBuilder().InGlobalNamespace().Build();

        [NotNull]
        protected abstract string GetSourceCode();

        public const string DefaultFilename = "Test.cs";

        [NotNull]
        [ItemNotNull]
        public static readonly ImmutableHashSet<MetadataReference> DefaultReferences =
            ImmutableHashSet.Create(new MetadataReference[]
            {
                /* mscorlib.dll */
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                /* System.dll */
                MetadataReference.CreateFromFile(typeof(Component).Assembly.Location)
            });

        public ParsedSourceCode Build()
        {
            ApplyNullability();

            string sourceText = GetCompleteSourceText();

            IList<string> nestedTypes = nullabilityAttributes?.NestedTypes ?? new string[0];

            return new ParsedSourceCode(sourceText, sourceFilename, settings, externalAnnotationsBuilder.Build(),
                ImmutableHashSet.Create(references.ToArray()), nestedTypes, codeNamespaceImportExpected, true);
        }

        private void ApplyNullability()
        {
            if (!string.IsNullOrEmpty(nullabilityAttributes?.CodeNamespace))
            {
                _ExpectingImportForNamespace(nullabilityAttributes.CodeNamespace);
            }
        }

        [NotNull]
        private string GetCompleteSourceText()
        {
            var sourceBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(headerText))
            {
                sourceBuilder.AppendLine(headerText);
            }

            bool hasNamespaceImportsAtTop = false;
            foreach (string ns in namespaceImports)
            {
                sourceBuilder.AppendLine($"using {ns};");
                hasNamespaceImportsAtTop = true;
            }

            if (!string.IsNullOrEmpty(codeNamespaceImportExpected))
            {
                sourceBuilder.AppendLine("<import/>");
                hasNamespaceImportsAtTop = true;
            }

            if (hasNamespaceImportsAtTop)
            {
                sourceBuilder.AppendLine();
            }

            sourceBuilder.Append(GetSourceCode());

            if (nullabilityAttributes != null)
            {
                sourceBuilder.Append(nullabilityAttributes.SourceText);
            }

            return sourceBuilder.ToString();
        }

        internal void _WithSettings([NotNull] AnalyzerSettingsBuilder settingsBuilder)
        {
            settings = settingsBuilder.Build();
        }

        internal void _Using([NotNull] string codeNamespace)
        {
            namespaceImports.Add(codeNamespace);
        }

        internal void _WithReference([NotNull] Assembly assembly)
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        internal void _WithReference([NotNull] Stream assemblyStream)
        {
            references.Add(MetadataReference.CreateFromStream(assemblyStream));
        }

        internal void _Named([NotNull] string filename)
        {
            sourceFilename = filename;
        }

        internal void _ExternallyAnnotated([NotNull] ExternalAnnotationsBuilder builder)
        {
            externalAnnotationsBuilder = builder;
        }

        internal void _WithNullabilityAttributes([NotNull] NullabilityAttributesBuilder builder)
        {
            nullabilityAttributes = builder.Build();
        }

        internal void _WithoutNullabilityAttributes()
        {
            nullabilityAttributes = null;
        }

        internal void _ExpectingImportForNamespace([NotNull] string codeNamespace)
        {
            codeNamespaceImportExpected = codeNamespace;
        }

        internal void _WithHeader([NotNull] string text)
        {
            headerText = text;
        }
    }

    public static class SourceCodeBuilderExtensions
    {
        [NotNull]
        public static TBuilder WithSettings<TBuilder>([NotNull] this TBuilder source,
            [NotNull] AnalyzerSettingsBuilder settingsBuilder)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(settingsBuilder, nameof(settingsBuilder));

            source._WithSettings(settingsBuilder);
            return source;
        }

        [NotNull]
        public static TBuilder Using<TBuilder>([NotNull] this TBuilder source, [CanBeNull] string codeNamespace)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            if (!string.IsNullOrWhiteSpace(codeNamespace))
            {
                source._Using(codeNamespace);
            }

            return source;
        }

        [NotNull]
        public static TBuilder WithHeader<TBuilder>([NotNull] this TBuilder source, [NotNull] string text)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(text, nameof(text));

            source._WithHeader(text);
            return source;
        }

        [NotNull]
        public static TBuilder WithReference<TBuilder>([NotNull] this TBuilder source, [NotNull] Assembly assembly)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(assembly, nameof(assembly));

            source._WithReference(assembly);
            return source;
        }

        [NotNull]
        public static TBuilder WithReferenceToExternalAssemblyFor<TBuilder>([NotNull] this TBuilder source,
            [NotNull] string code) where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(code, nameof(code));

            Stream assemblyStream = GetInMemoryAssemblyStreamForCode(code);
            source._WithReference(assemblyStream);
            return source;
        }

        [NotNull]
        private static Stream GetInMemoryAssemblyStreamForCode([NotNull] string code,
            [NotNull] [ItemNotNull] params MetadataReference[] references)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation =
                CSharpCompilation.Create("TempAssembly", new[] { tree })
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            PortableExecutableReference msCorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            compilation = compilation.AddReferences(msCorLib);
            compilation = compilation.AddReferences(references);

            var stream = new MemoryStream();

            EmitResult emitResult = compilation.Emit(stream);
            ValidateCompileErrors(emitResult);

            return stream;
        }

        private static void ValidateCompileErrors([NotNull] EmitResult emitResult)
        {
            Diagnostic[] compilerErrors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            compilerErrors.Should().BeEmpty("external assembly should not have compile errors");
            emitResult.Success.Should().BeTrue();
        }

        [NotNull]
        public static TBuilder Named<TBuilder>([NotNull] this TBuilder source, [NotNull] string filename)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(filename, nameof(filename));

            source._Named(filename);
            return source;
        }

        [NotNull]
        public static TBuilder ExternallyAnnotated<TBuilder>([NotNull] this TBuilder source,
            [NotNull] ExternalAnnotationsBuilder builder) where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(builder, nameof(builder));

            source._ExternallyAnnotated(builder);
            return source;
        }

        [NotNull]
        public static TBuilder WithNullabilityAttributes<TBuilder>([NotNull] this TBuilder source,
            [NotNull] NullabilityAttributesBuilder builder) where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(builder, nameof(builder));

            source._WithNullabilityAttributes(builder);
            return source;
        }

        [NotNull]
        public static TBuilder WithoutNullabilityAttributes<TBuilder>([NotNull] this TBuilder source)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));

            source._WithoutNullabilityAttributes();
            return source;
        }

        [NotNull]
        public static TBuilder ExpectingImportForNamespace<TBuilder>([NotNull] this TBuilder source,
            [NotNull] string expectedImportText) where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(expectedImportText, nameof(expectedImportText));

            source._ExpectingImportForNamespace(expectedImportText);
            return source;
        }
    }
}
