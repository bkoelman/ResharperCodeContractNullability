using System;
using System.Collections.Generic;
using System.Linq;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;

namespace CodeContractNullability.Test.RoslynTestFramework
{
    public sealed class FixableDocument
    {
        // Supported markers:
        //
        //   [+demo+] expects "demo" will be inserted
        //   [-demo-] expects "demo" will be removed
        //   [*before##after*] expects that "before" will be replaced with "after"
        //
        // Example:
        // Input:    abc[+INS+]def[-DEL-]ghi[*YYY##ZZZ*]jkl
        // Source:   abcdefDELghiYYYjkl
        // Expected: abcINSdefghiZZZjkl

        [NotNull]
        [ItemNotNull]
        private readonly IList<TextBlock> blocks;

        [NotNull]
        public string SourceText => string.Concat(blocks.Select(x => x.TextBefore));

        [NotNull]
        public string ExpectedText => string.Concat(blocks.Select(x => x.TextAfter));

        public FixableDocument([NotNull] string text)
        {
            Guard.NotNull(text, nameof(text));

            var parser = new MarkupParser(text);
            blocks = parser.Parse();
        }

        private sealed class MarkupParser
        {
            private const string SpanOpenText = "[";
            private const string SpanCloseText = "]";
            private const string ReplaceSeparator = "##";
            private const int SpanTextLength = 2;

            [NotNull]
            private static readonly char[] SpanKinds = { '+', '-', '*' };

            [NotNull]
            [ItemNotNull]
            private static readonly string[] ReplaceSeparatorArray = { ReplaceSeparator };

            [NotNull]
            private readonly string markupCode;

            [NotNull]
            [ItemNotNull]
            private readonly IList<TextBlock> blocks = new List<TextBlock>();

            public MarkupParser([NotNull] string markupCode)
            {
                Guard.NotNull(markupCode, nameof(markupCode));
                this.markupCode = markupCode;
            }

            [NotNull]
            [ItemNotNull]
            public IList<TextBlock> Parse()
            {
                blocks.Clear();

                ParseMarkupCode();

                return blocks;
            }

            private void ParseMarkupCode()
            {
                var stateMachine = new ParseStateMachine(this);
                stateMachine.Run();
            }

            private int GetNextSpanStart(int offset, out char spanKind)
            {
                while (true)
                {
                    int index = markupCode.IndexOf(SpanOpenText, offset, StringComparison.Ordinal);

                    if (index == -1 || markupCode.Length < index + 1)
                    {
                        spanKind = '?';
                        return -1;
                    }

                    spanKind = markupCode[index + 1];
                    if (SpanKinds.Contains(spanKind))
                    {
                        return index;
                    }

                    offset = index + 1;
                }
            }

            private int GetNextSpanEnd(int start, char spanKind)
            {
                string spanEndText = spanKind + SpanCloseText;

                int index = markupCode.IndexOf(spanEndText, start + SpanTextLength, StringComparison.Ordinal);
                if (index == -1)
                {
                    throw new Exception($"Missing '{spanEndText}' in source.");
                }

                return index;
            }

            private void AppendCodeBlock(int offset, int length)
            {
                if (length > 0)
                {
                    string text = markupCode.Substring(offset, length);
                    blocks.Add(new FixedTextBlock(text));
                }
            }

            private void AppendTextSpan(int spanStartIndex, int spanEndIndex, char spanKind)
            {
                string spanInnerText = markupCode.Substring(spanStartIndex + SpanTextLength,
                    spanEndIndex - spanStartIndex - SpanTextLength);

                if (spanInnerText.Length == 0)
                {
                    return;
                }

                switch (spanKind)
                {
                    case '+':
                    {
                        blocks.Add(new InsertedTextBlock(spanInnerText));
                        break;
                    }
                    case '-':
                    {
                        blocks.Add(new DeletedTextBlock(spanInnerText));
                        break;
                    }
                    case '*':
                    {
                        var parts = spanInnerText.Split(ReplaceSeparatorArray, StringSplitOptions.None);

                        if (parts.Length == 1)
                        {
                            throw new Exception($"Missing '{ReplaceSeparator}' in source.");
                        }

                        if (parts.Length > 2)
                        {
                            throw new Exception($"Multiple '{ReplaceSeparator}' in source.");
                        }

                        blocks.Add(new ReplacedTextBlock(parts[0], parts[1]));
                        break;
                    }
                }
            }

            private void AppendLastCodeBlock(int offset)
            {
                AssertSpanIsClosed(offset);

                string text = markupCode.Substring(offset);
                if (text.Length > 0)
                {
                    blocks.Add(new FixedTextBlock(text));
                }
            }

            private void AssertSpanIsClosed(int offset)
            {
                foreach (var spanKind in SpanKinds)
                {
                    string spanEndText = spanKind + SpanCloseText;

                    int index = markupCode.IndexOf(spanEndText, offset + SpanTextLength, StringComparison.Ordinal);
                    if (index != -1)
                    {
                        throw new Exception($"Additional '{spanEndText}' found in source.");
                    }
                }
            }

            private struct ParseStateMachine
            {
                [NotNull]
                private readonly MarkupParser parser;

                private int offset;
                private int spanStartIndex;
                private int spanEndIndex;
                private char spanKind;

                private bool HasFoundSpanStart => spanStartIndex != -1;

                public ParseStateMachine([NotNull] MarkupParser parser)
                {
                    Guard.NotNull(parser, nameof(parser));

                    this.parser = parser;
                    offset = -1;
                    spanStartIndex = -1;
                    spanEndIndex = -1;
                    spanKind = '?';
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
                    spanStartIndex = parser.GetNextSpanStart(offset, out spanKind);
                }

                private void LoopOverText()
                {
                    while (HasFoundSpanStart)
                    {
                        AppendCodeBlockBeforeSpanStart();

                        LocateNextSpanEnd();

                        AppendTextSpan();

                        LocateNextSpanStart();
                    }
                }

                private void AppendCodeBlockBeforeSpanStart()
                {
                    int length = spanStartIndex - offset;
                    parser.AppendCodeBlock(offset, length);
                }

                private void LocateNextSpanEnd()
                {
                    spanEndIndex = parser.GetNextSpanEnd(spanStartIndex, spanKind);
                }

                private void AppendTextSpan()
                {
                    parser.AppendTextSpan(spanStartIndex, spanEndIndex, spanKind);
                }

                private void LocateNextSpanStart()
                {
                    offset = spanEndIndex + SpanTextLength;
                    spanStartIndex = parser.GetNextSpanStart(offset, out spanKind);
                }

                private void AppendCodeBlockAfterSpanEnd()
                {
                    parser.AppendLastCodeBlock(offset);
                }
            }
        }

        private abstract class TextBlock
        {
            [NotNull]
            public string TextBefore { get; }

            [NotNull]
            public string TextAfter { get; }

            protected TextBlock([NotNull] string textBefore, [NotNull] string textAfter)
            {
                Guard.NotNull(textBefore, nameof(textBefore));
                Guard.NotNull(textAfter, nameof(textAfter));

                TextBefore = textBefore;
                TextAfter = textAfter;
            }
        }

        private sealed class FixedTextBlock : TextBlock
        {
            public FixedTextBlock([NotNull] string text)
                : base(text, text)
            {
            }

            public override string ToString() => TextBefore;
        }

        private sealed class InsertedTextBlock : TextBlock
        {
            public InsertedTextBlock([NotNull] string textToInsert)
                : base(string.Empty, textToInsert)
            {
            }

            public override string ToString() => "+" + TextAfter;
        }

        private sealed class DeletedTextBlock : TextBlock
        {
            public DeletedTextBlock([NotNull] string textToDelete)
                : base(textToDelete, string.Empty)
            {
            }

            public override string ToString() => "-" + TextBefore;
        }

        private sealed class ReplacedTextBlock : TextBlock
        {
            public ReplacedTextBlock([NotNull] string textBefore, [NotNull] string textAfter)
                : base(textBefore, textAfter)
            {
            }

            public override string ToString() => TextBefore + "=>" + TextAfter;
        }
    }
}
