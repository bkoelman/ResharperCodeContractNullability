using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

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
        private string sourceFileName = DefaultFileName;

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

        public const string DefaultFileName = "Test.cs";

        [NotNull]
        [ItemNotNull]
        public static readonly ImmutableHashSet<MetadataReference> DefaultReferences = GetDefaultReferences();

        [NotNull]
        [ItemNotNull]
        private static ImmutableHashSet<MetadataReference> GetDefaultReferences()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            return ImmutableHashSet.Create(new MetadataReference[]
            {
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll"))
            });
        }

        public ParsedSourceCode Build()
        {
            ApplyNullability();

            string sourceText = GetCompleteSourceText();

            IList<string> nestedTypes = nullabilityAttributes?.NestedTypes ?? new string[0];

            return new ParsedSourceCode(sourceText, sourceFileName, settings, externalAnnotationsBuilder.Build(),
                ImmutableHashSet.Create(references.ToArray()), nestedTypes, true);
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
                sourceBuilder.AppendLine("[+using " + codeNamespaceImportExpected + ";+]");
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

        [NotNull]
        protected string GetLinesOfCode([NotNull] [ItemNotNull] IEnumerable<string> codeBlocks)
        {
            Guard.NotNull(codeBlocks, nameof(codeBlocks));

            var builder = new StringBuilder();

            bool isInFirstBlock = true;
            foreach (string codeBlock in codeBlocks)
            {
                if (isInFirstBlock)
                {
                    isInFirstBlock = false;
                }
                else
                {
                    builder.AppendLine();
                }

                bool isOnFirstLineInBlock = true;
                using (var reader = new StringReader(codeBlock.TrimEnd()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isOnFirstLineInBlock)
                        {
                            if (line.Trim().Length == 0)
                            {
                                continue;
                            }

                            isOnFirstLineInBlock = false;
                        }

                        builder.AppendLine(line);
                    }
                }
            }

            return builder.ToString();
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

        internal void _InFileNamed([NotNull] string filename)
        {
            sourceFileName = filename;
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
}
