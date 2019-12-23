using System.Collections.Immutable;
using System.Text;
using System.Threading;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability.Test.TestDataBuilders
{
    internal sealed class AnalyzerSettingsBuilder : ITestDataBuilder<AnalyzerSettings>
    {
        private bool disableReportOnNullableValueTypes;

        [NotNull]
        public AnalyzerSettingsBuilder DisableReportOnNullableValueTypes
        {
            get
            {
                disableReportOnNullableValueTypes = true;
                return this;
            }
        }

        public AnalyzerSettings Build()
        {
            return new AnalyzerSettings(disableReportOnNullableValueTypes);
        }

        [NotNull]
        public static AnalyzerOptions ToOptions([NotNull] AnalyzerSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));

            string content = SettingsProvider.ToFileContent(settings);
            return ToOptions(content);
        }

        [NotNull]
        public static AnalyzerOptions ToOptions([NotNull] string settingsText)
        {
            Guard.NotNull(settingsText, nameof(settingsText));

            AdditionalText additionalText = new FakeAdditionalText(settingsText);
            return new AnalyzerOptions(ImmutableArray.Create(additionalText));
        }

        private sealed class FakeAdditionalText : AdditionalText
        {
            [NotNull]
            private readonly SourceText sourceText;

            [NotNull]
            public override string Path { get; } = SettingsProvider.SettingsFileName;

            public FakeAdditionalText([NotNull] string content)
            {
                sourceText = new FakeSourceText(content, SettingsProvider.CreateEncoding());
            }

            [NotNull]
            public override SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
            {
                return sourceText;
            }

            private sealed class FakeSourceText : SourceText
            {
                [NotNull]
                private readonly string content;

                [NotNull]
                public override Encoding Encoding { get; }

                public override int Length => content.Length;

                public override char this[int position] => content[position];

                public FakeSourceText([NotNull] string content, [NotNull] Encoding encoding)
                {
                    Guard.NotNull(content, nameof(content));
                    Guard.NotNull(encoding, nameof(encoding));

                    this.content = content;
                    Encoding = encoding;
                }

                public override void CopyTo(int sourceIndex, [NotNull] char[] destination, int destinationIndex, int count)
                {
                    content.CopyTo(sourceIndex, destination, destinationIndex, count);
                }
            }
        }
    }
}
