﻿using System;
using System.Collections.Generic;
using System.Text;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
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
        public DocumentWithSpans GetDocumentWithSpansFromMarkup([NotNull] AnalyzerTestContext context)
        {
            Guard.NotNull(context, nameof(context));

            var parser = new MarkupParser(context.MarkupCode);
            CodeWithSpans codeWithSpans = parser.Parse();

            ParseOptions parseOptions = GetParseOptions(context.DocumentationMode, context.LanguageName);
            CompilationOptions compilationOptions = GetCompilationOptions(context.CompilerWarningLevel, context.LanguageName);

            Document document = new AdhocWorkspace()
                .AddProject(context.AssemblyName, context.LanguageName)
                .WithParseOptions(parseOptions)
                .WithCompilationOptions(compilationOptions)
                .AddMetadataReferences(context.References)
                .AddDocument(context.FileName, codeWithSpans.Code);

            return new DocumentWithSpans(document, codeWithSpans.Spans);
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
        public string FormatSourceCode([NotNull] string sourceCode, [NotNull] AnalyzerTestContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(sourceCode, nameof(sourceCode));

            Document document = GetDocumentWithSpansFromMarkup(context.WithMarkupCode(sourceCode)).Document;

            return FormatDocument(document);
        }

        [NotNull]
        public string FormatDocument([NotNull] Document document)
        {
            Guard.NotNull(document, nameof(document));

            SyntaxNode syntaxRoot = document.GetSyntaxRootAsync().Result;

            SyntaxNode formattedSyntaxRoot = Formatter.Format(syntaxRoot, document.Project.Solution.Workspace);
            return formattedSyntaxRoot.ToFullString();
        }

        private struct CodeWithSpans
        {
            [NotNull]
            public string Code { get; }

            [NotNull]
            public IList<TextSpan> Spans { get; }

            public CodeWithSpans([NotNull] string code, [NotNull] IList<TextSpan> spans)
            {
                Code = code;
                Spans = spans;
            }
        }

        private sealed class MarkupParser
        {
            private const string SpanStartText = "[|";
            private const string SpanEndText = "|]";
            private const int SpanTextLength = 2;

            [NotNull]
            private readonly string markupCode;

            [NotNull]
            private readonly StringBuilder codeBuilder;

            [NotNull]
            private readonly IList<TextSpan> textSpans = new List<TextSpan>();

            public MarkupParser([NotNull] string markupCode)
            {
                Guard.NotNull(markupCode, nameof(markupCode));

                this.markupCode = markupCode;
                codeBuilder = new StringBuilder(markupCode.Length);
            }

            public CodeWithSpans Parse()
            {
                codeBuilder.Clear();
                textSpans.Clear();

                ParseMarkupCode();

                return new CodeWithSpans(codeBuilder.ToString(), textSpans);
            }

            private void ParseMarkupCode()
            {
                var stateMachine = new ParseStateMachine(this);
                stateMachine.Run();
            }

            private int GetNextSpanStart(int offset)
            {
                return markupCode.IndexOf(SpanStartText, offset, StringComparison.Ordinal);
            }

            private int GetNextSpanEnd(int start)
            {
                int end = markupCode.IndexOf(SpanEndText, start + SpanTextLength, StringComparison.Ordinal);
                if (end == -1)
                {
                    throw new Exception($"Missing {SpanEndText} in source.");
                }
                return end;
            }

            private void AppendCodeBlock(int offset, int length)
            {
                codeBuilder.Append(markupCode.Substring(offset, length));
            }

            private void AppendTextSpan(int spanStartIndex, int spanEndIndex)
            {
                int shift = textSpans.Count * (SpanTextLength + SpanTextLength);
                textSpans.Add(TextSpan.FromBounds(spanStartIndex - shift, spanEndIndex - SpanTextLength - shift));
            }

            private void AppendLastCodeBlock(int offset)
            {
                AssertSpanIsClosed(offset);

                codeBuilder.Append(markupCode.Substring(offset));
            }

            private void AssertSpanIsClosed(int offset)
            {
                int extra = markupCode.IndexOf(SpanEndText, offset, StringComparison.Ordinal);
                if (extra != -1)
                {
                    throw new Exception($"Additional {SpanEndText} found in source.");
                }
            }

            private struct ParseStateMachine
            {
                [NotNull]
                private readonly MarkupParser parser;

                private int offset;
                private int spanStartIndex;
                private int spanEndIndex;

                private bool HasFoundSpanStart => spanStartIndex != -1;

                public ParseStateMachine([NotNull] MarkupParser parser)
                {
                    Guard.NotNull(parser, nameof(parser));

                    this.parser = parser;
                    offset = -1;
                    spanStartIndex = -1;
                    spanEndIndex = -1;
                }

                public void Run()
                {
                    LocateFirstSpanStart();

                    LoopOverText();

                    AppendCodeBlockAfterSpanEnd();
                }

                private void LocateFirstSpanStart()
                {
                    offset = 0;
                    spanStartIndex = parser.GetNextSpanStart(offset);
                }

                private void LoopOverText()
                {
                    while (HasFoundSpanStart)
                    {
                        AppendCodeBlockBeforeSpanStart();

                        LocateNextSpanEnd();

                        AppendCodeBlockBetweenSpans();
                        AppendTextSpan();

                        LocateNextSpanStart();
                    }
                }

                private void AppendCodeBlockBeforeSpanStart()
                {
                    parser.AppendCodeBlock(offset, spanStartIndex - offset);
                }

                private void LocateNextSpanEnd()
                {
                    spanEndIndex = parser.GetNextSpanEnd(spanStartIndex);
                }

                private void AppendCodeBlockBetweenSpans()
                {
                    parser.AppendCodeBlock(spanStartIndex + SpanTextLength,
                        spanEndIndex - spanStartIndex - SpanTextLength);
                }

                private void AppendTextSpan()
                {
                    parser.AppendTextSpan(spanStartIndex, spanEndIndex);
                }

                private void LocateNextSpanStart()
                {
                    offset = spanEndIndex + SpanTextLength;
                    spanStartIndex = parser.GetNextSpanStart(offset);
                }

                private void AppendCodeBlockAfterSpanEnd()
                {
                    parser.AppendLastCodeBlock(offset);
                }
            }
        }
    }
}
