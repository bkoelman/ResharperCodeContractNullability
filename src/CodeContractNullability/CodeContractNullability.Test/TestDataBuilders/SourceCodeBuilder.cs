using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public abstract class SourceCodeBuilder : ITestDataBuilder<ParsedSourceCode>
    {
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
        private NullabilityAttributesDefinition nullabilityAttributes;

        [NotNull]
        protected abstract string GetSourceCode();

        public const string DefaultFilename = "Test.cs";

        [NotNull]
        [ItemNotNull]
        public static readonly ImmutableHashSet<MetadataReference> DefaultReferences = ImmutableHashSet.Create(
            new MetadataReference[]
            {
                /* mscorlib.dll */
                MetadataReference.CreateFromFile(typeof (object).Assembly.Location),
                /* System.dll */
                MetadataReference.CreateFromFile(typeof (Component).Assembly.Location)
            });

        public ParsedSourceCode Build()
        {
            ApplyNullability();

            string sourceText = GetCompleteSourceText();

            return new ParsedSourceCode(sourceText, sourceFilename, externalAnnotationsBuilder.Build(),
                ImmutableHashSet.Create(references.ToArray()), codeNamespaceImportExpected, true);
        }

        private void ApplyNullability()
        {
            if (!string.IsNullOrEmpty(nullabilityAttributes?.CodeNamespace))
            {
                if (nullabilityAttributes.Imported)
                {
                    _Using(nullabilityAttributes.CodeNamespace);
                }
                else
                {
                    _ExpectingImportForNamespace(nullabilityAttributes.CodeNamespace);
                }
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

        internal void _Using([NotNull] string codeNamespace)
        {
            namespaceImports.Add(codeNamespace);
        }

        internal void _WithReference([NotNull] Assembly assembly)
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        internal void _WithReference([NotNull] MetadataReference reference)
        {
            references.Add(reference);
        }

        internal void _Named([NotNull] string filename)
        {
            sourceFilename = filename;
        }

        internal void _ExternallyAnnotated([NotNull] ExternalAnnotationsBuilder builder)
        {
            externalAnnotationsBuilder = builder;
        }

        internal void _WithNullability([NotNull] NullabilityAttributesBuilder builder)
        {
            nullabilityAttributes = builder.Build();
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
        public static TBuilder Using<TBuilder>([NotNull] this TBuilder source, [NotNull] string codeNamespace)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(codeNamespace, nameof(codeNamespace));

            source._Using(codeNamespace);
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
            [NotNull] string code)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(code, nameof(code));

            MetadataReference reference = GetInMemoryAssemblyReferenceForCode(code);
            source._WithReference(reference);
            return source;
        }

        [NotNull]
        private static MetadataReference GetInMemoryAssemblyReferenceForCode([NotNull] string code,
            [NotNull] [ItemNotNull] params MetadataReference[] references)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation =
                CSharpCompilation.Create("TempAssembly", new[] { tree })
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            PortableExecutableReference msCorLib = MetadataReference.CreateFromFile(typeof (object).Assembly.Location);
            compilation = compilation.AddReferences(msCorLib);
            compilation = compilation.AddReferences(references);

            return compilation.ToMetadataReference();
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
            [NotNull] ExternalAnnotationsBuilder builder)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(builder, nameof(builder));

            source._ExternallyAnnotated(builder);
            return source;
        }

        [NotNull]
        public static TBuilder WithNullabilityAttributes<TBuilder>([NotNull] this TBuilder source,
            [NotNull] NullabilityAttributesBuilder builder)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(builder, nameof(builder));

            source._WithNullability(builder);
            return source;
        }

        [NotNull]
        public static TBuilder ExpectingImportForNamespace<TBuilder>([NotNull] this TBuilder source,
            [NotNull] string expectedImportText)
            where TBuilder : SourceCodeBuilder
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(expectedImportText, nameof(expectedImportText));

            source._ExpectingImportForNamespace(expectedImportText);
            return source;
        }
    }
}