using System.IO;
using System.Linq;
using System.Reflection;
using CodeContractNullability.Utilities;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public static class SourceCodeBuilderExtensions
    {
        [NotNull]
        public static TBuilder WithHeader<TBuilder>([NotNull] this TBuilder source, [NotNull] string text)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(text, nameof(text));

            source.Editor.WithHeaderText(text);

            return source;
        }

        [NotNull]
        public static TBuilder Using<TBuilder>([NotNull] this TBuilder source, [CanBeNull] string codeNamespace)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));

            if (!string.IsNullOrWhiteSpace(codeNamespace))
            {
                source.Editor.IncludeNamespaceImport(codeNamespace);
            }

            return source;
        }

        [NotNull]
        public static TBuilder WithNullabilityAttributes<TBuilder>([NotNull] this TBuilder source,
            [NotNull] NullabilityAttributesBuilder builder)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(builder, nameof(builder));

            source.Editor.SetNullabilityAttributes(builder);

            return source;
        }

        [NotNull]
        public static TBuilder WithoutNullabilityAttributes<TBuilder>([NotNull] this TBuilder source)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));

            source.Editor.SetNullabilityAttributes(null);

            return source;
        }

        [NotNull]
        public static TBuilder InFileNamed<TBuilder>([NotNull] this TBuilder source, [NotNull] string fileName)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNullNorWhiteSpace(fileName, nameof(fileName));

            source.Editor.UpdateTestContext(context => context.InFileNamed(fileName));

            return source;
        }

        [NotNull]
        public static TBuilder WithReference<TBuilder>([NotNull] this TBuilder source, [NotNull] Assembly assembly)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(assembly, nameof(assembly));

            PortableExecutableReference reference = MetadataReference.CreateFromFile(assembly.Location);

            source.Editor.UpdateTestContext(context => context.WithReferences(context.References.Add(reference)));

            return source;
        }

        [NotNull]
        public static TBuilder WithReferenceToExternalAssemblyFor<TBuilder>([NotNull] this TBuilder source, [NotNull] string code)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(code, nameof(code));

            Stream assemblyStream = GetInMemoryAssemblyStreamForCode(code);
            PortableExecutableReference reference = MetadataReference.CreateFromStream(assemblyStream);

            source.Editor.UpdateTestContext(context => context.WithReferences(context.References.Add(reference)));

            return source;
        }

        [NotNull]
        private static Stream GetInMemoryAssemblyStreamForCode([NotNull] string code,
            [NotNull] [ItemNotNull] params MetadataReference[] references)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation = CSharpCompilation
                .Create("TempAssembly", new[] { tree })
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            compilation = compilation.AddReferences(SourceCodeBuilder.DefaultTestContext.References);
            compilation = compilation.AddReferences(references);

            var stream = new MemoryStream();

            EmitResult emitResult = compilation.Emit(stream);
            ValidateCompileErrors(emitResult);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private static void ValidateCompileErrors([NotNull] EmitResult emitResult)
        {
            Diagnostic[] compilerErrors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            compilerErrors.Should().BeEmpty("external assembly should not have compile errors");
            emitResult.Success.Should().BeTrue();
        }

        [NotNull]
        public static TBuilder ExternallyAnnotated<TBuilder>([NotNull] this TBuilder source,
            [NotNull] ExternalAnnotationsBuilder builder)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(builder, nameof(builder));

            source.Editor.WithExternalAnnotations(builder);

            return source;
        }

        [NotNull]
        public static TBuilder WithSettings<TBuilder>([NotNull] this TBuilder source,
            [NotNull] AnalyzerSettingsBuilder settingsBuilder)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(settingsBuilder, nameof(settingsBuilder));

            AnalyzerSettings settings = settingsBuilder.Build();
            AnalyzerOptions options = AnalyzerSettingsBuilder.ToOptions(settings);

            source.Editor.UpdateTestContext(context => context.WithOptions(options));

            return source;
        }

        [NotNull]
        public static TBuilder ExpectingImportForNamespace<TBuilder>([NotNull] this TBuilder source,
            [NotNull] string expectedImportText)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(expectedImportText, nameof(expectedImportText));

            source.Editor.ExpectingImportForNamespace(expectedImportText);

            return source;
        }
    }
}
