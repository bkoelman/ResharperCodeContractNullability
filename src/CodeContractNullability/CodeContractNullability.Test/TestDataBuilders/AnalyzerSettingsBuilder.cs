using System.Collections.Immutable;
using System.Text;
using System.Threading;
using CodeContractNullability.Settings;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public sealed class AnalyzerSettingsBuilder : ITestDataBuilder<AnalyzerSettings>
    {
        private bool disableReportOnNullableValueTypes;
        private TypeHierarchyReportMode typeHierarchyReportMode;

        public AnalyzerSettings Build()
        {
            return new AnalyzerSettings(disableReportOnNullableValueTypes, typeHierarchyReportMode);
        }

        [NotNull]
        public AnalyzerSettingsBuilder DisableReportOnNullableValueTypes
        {
            get
            {
                disableReportOnNullableValueTypes = true;
                return this;
            }
        }

        [NotNull]
        public AnalyzerSettingsBuilder InTypeHierarchyReportMode(TypeHierarchyReportMode mode)
        {
            typeHierarchyReportMode = mode;
            return this;
        }

        [NotNull]
        public static AnalyzerOptions ToOptions([NotNull] AnalyzerSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));

            string content = SettingsProvider.ToFileContent(settings);
            return new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(new FakeAdditionalText(content)));
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
