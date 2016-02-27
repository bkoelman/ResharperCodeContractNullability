using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability
{
    internal sealed class SettingsReader
    {
        [CanBeNull]
        private readonly AdditionalText settingsFileOrNull;

        public SettingsReader([NotNull] AnalyzerOptions options)
        {
            Guard.NotNull(options, nameof(options));

            settingsFileOrNull = options.AdditionalFiles.FirstOrDefault(IsSettingsFile);
        }

        private static bool IsSettingsFile([NotNull] AdditionalText file)
        {
            string fileName = Path.GetFileName(file.Path);
            return string.Equals(fileName, "ResharperCodeContractNullability.config", StringComparison.OrdinalIgnoreCase);
        }

        [NotNull]
        public AnalyzerSettings GetSettings(CancellationToken cancellationToken)
        {
            if (settingsFileOrNull != null)
            {
                SourceText fileText = settingsFileOrNull.GetText(cancellationToken);

                try
                {
                    return ReadSourceText(fileText, reader =>
                    {
                        var serializer = new DataContractSerializer(typeof (AnalyzerSettings));
                        return (AnalyzerSettings) serializer.ReadObject(reader);
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.Write("Failed to parse analyser settings file. Using default settings. Exception: " + ex);
                }
            }

            return AnalyzerSettings.Default;
        }

        [NotNull]
        private static TResult ReadSourceText<TResult>([NotNull] SourceText sourceText,
            [NotNull] Func<XmlReader, TResult> readAction, CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    sourceText.Write(writer, cancellationToken);
                    writer.Flush();

                    stream.Seek(0, SeekOrigin.Begin);

                    using (var textReader = new StreamReader(stream))
                    {
                        using (XmlReader xmlReader = XmlReader.Create(textReader))
                        {
                            return readAction(xmlReader);
                        }
                    }
                }
            }
        }
    }
}