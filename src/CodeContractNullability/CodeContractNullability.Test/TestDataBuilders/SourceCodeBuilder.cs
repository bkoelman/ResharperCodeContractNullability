using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RoslynTestFramework;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal abstract class SourceCodeBuilder : ITestDataBuilder<ParsedSourceCode>
    {
        [NotNull]
        public static readonly AnalyzerTestContext DefaultTestContext = new AnalyzerTestContext(string.Empty,
            Array.Empty<TextSpan>(), LanguageNames.CSharp, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));

        [NotNull]
        private AnalyzerTestContext testContext = DefaultTestContext;

        [CanBeNull]
        private string headerText;

        [NotNull]
        [ItemNotNull]
        private readonly HashSet<string> namespaceImports = new HashSet<string>
        {
            "System"
        };

        [CanBeNull]
        private NullabilityAttributesDefinition nullabilityAttributes =
            new NullabilityAttributesBuilder().InGlobalNamespace().Build();

        [NotNull]
        private ExternalAnnotationsBuilder externalAnnotationsBuilder = new ExternalAnnotationsBuilder();

        [NotNull]
        private string codeNamespaceImportExpected = string.Empty;

        [NotNull]
        internal readonly CodeEditor Editor;

        protected SourceCodeBuilder()
        {
            Editor = new CodeEditor(this);
        }

        public ParsedSourceCode Build()
        {
            ApplyNullability();

            string sourceText = GetCompleteSourceText();

            IList<string> nestedTypes = nullabilityAttributes?.NestedTypes ?? new string[0];

            return new ParsedSourceCode(sourceText, testContext, externalAnnotationsBuilder.Build(), nestedTypes,
                TextComparisonMode.IgnoreWhitespaceDifferences);
        }

        private void ApplyNullability()
        {
            if (!string.IsNullOrEmpty(nullabilityAttributes?.CodeNamespace))
            {
                Editor.ExpectingImportForNamespace(nullabilityAttributes.CodeNamespace);
            }
        }

        [NotNull]
        private string GetCompleteSourceText()
        {
            var sourceBuilder = new StringBuilder();

            WriteHeaderText(sourceBuilder);
            WriteNamespaceImports(sourceBuilder);

            sourceBuilder.Append(GetSourceCode());

            WriteNullabilityAttributes(sourceBuilder);

            return sourceBuilder.ToString();
        }

        private void WriteHeaderText([NotNull] StringBuilder sourceBuilder)
        {
            if (!string.IsNullOrEmpty(headerText))
            {
                sourceBuilder.AppendLine(headerText);
            }
        }

        private void WriteNamespaceImports([NotNull] StringBuilder sourceBuilder)
        {
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
        }

        private void WriteNullabilityAttributes([NotNull] StringBuilder sourceBuilder)
        {
            if (nullabilityAttributes != null)
            {
                sourceBuilder.Append(nullabilityAttributes.SourceText);
            }
        }

        [NotNull]
        protected abstract string GetSourceCode();

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

        internal sealed class CodeEditor
        {
            [NotNull]
            private readonly SourceCodeBuilder owner;

            public CodeEditor([NotNull] SourceCodeBuilder owner)
            {
                Guard.NotNull(owner, nameof(owner));
                this.owner = owner;
            }

            public void UpdateTestContext([NotNull] Func<AnalyzerTestContext, AnalyzerTestContext> change)
            {
                Guard.NotNull(change, nameof(change));

                owner.testContext = change(owner.testContext);
            }

            public void WithHeaderText([NotNull] string text)
            {
                owner.headerText = text;
            }

            public void IncludeNamespaceImport([NotNull] string codeNamespace)
            {
                owner.namespaceImports.Add(codeNamespace);
            }

            public void SetNullabilityAttributes([CanBeNull] NullabilityAttributesBuilder builder)
            {
                owner.nullabilityAttributes = builder?.Build();
            }

            public void WithExternalAnnotations([NotNull] ExternalAnnotationsBuilder builder)
            {
                owner.externalAnnotationsBuilder = builder;
            }

            public void ExpectingImportForNamespace([NotNull] string codeNamespace)
            {
                owner.codeNamespaceImportExpected = codeNamespace;
            }
        }
    }
}
