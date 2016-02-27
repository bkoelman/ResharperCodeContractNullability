using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability.Test.TestDataBuilders
{
    public class AnalyzerSettingsBuilder : ITestDataBuilder<AnalyzerSettings>
    {
        private bool disableReportOnNullableValueTypes;

        public AnalyzerSettings Build()
        {
            return new AnalyzerSettings(disableReportOnNullableValueTypes);
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
        public static AnalyzerOptions ToOptions([NotNull] AnalyzerSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));

            return new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(new FakeAdditionalText(settings)));
        }

        private sealed class FakeAdditionalText : AdditionalText
        {
            [NotNull]
            private readonly Encoding encoding = new UTF8Encoding();

            [NotNull]
            private readonly SourceText sourceText;

            [NotNull]
            public override string Path { get; } = "ResharperCodeContractNullability.config";

            public FakeAdditionalText([NotNull] AnalyzerSettings settings)
            {
                Guard.NotNull(settings, nameof(settings));

                string text = GetStringForXml(encoding, writer =>
                {
                    var serializer = new DataContractSerializer(typeof (AnalyzerSettings));
                    serializer.WriteObject(writer, settings);
                });

                sourceText = new FakeSourceText(text, encoding);
            }

            [NotNull]
            private static string GetStringForXml([NotNull] Encoding encoding, [NotNull] Action<XmlWriter> writeAction)
            {
                using (var stream = new MemoryStream())
                {
                    using (var textWriter = new StreamWriter(stream))
                    {
                        var xmlSettings = new XmlWriterSettings { Encoding = encoding };
                        using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, xmlSettings))
                        {
                            writeAction(xmlWriter);

                            xmlWriter.Flush();
                            textWriter.Flush();

                            stream.Seek(0, SeekOrigin.Begin);

                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }

            [NotNull]
            public override SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
            {
                return sourceText;
            }

            private sealed class FakeSourceText : SourceText
            {
                [NotNull]
                private readonly string text;

                [NotNull]
                public override Encoding Encoding { get; }

                public override int Length => text.Length;

                public override char this[int position] => text[position];

                public FakeSourceText([NotNull] string text, [NotNull] Encoding encoding)
                {
                    Guard.NotNull(text, nameof(text));
                    Guard.NotNull(encoding, nameof(encoding));

                    this.text = text;
                    Encoding = encoding;
                }

                public override void CopyTo(int sourceIndex, [NotNull] char[] destination, int destinationIndex,
                    int count)
                {
                    text.CopyTo(sourceIndex, destination, destinationIndex, count);
                }
            }
        }
    }
}