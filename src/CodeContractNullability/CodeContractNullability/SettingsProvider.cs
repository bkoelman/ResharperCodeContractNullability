using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeContractNullability
{
    public static class SettingsProvider
    {
        public const string SettingsFileName = "ResharperCodeContractNullability.config";

        [NotNull]
        public static Encoding CreateEncoding()
        {
            return new UTF8Encoding();
        }

        [NotNull]
        internal static AnalyzerSettings LoadSettings([NotNull] AnalyzerOptions options, CancellationToken cancellationToken)
        {
            Guard.NotNull(options, nameof(options));

            AdditionalText settingsFileOrNull = options.AdditionalFiles.FirstOrDefault(file => IsSettingsFile(file.Path));

            if (settingsFileOrNull != null)
            {
                SourceText fileText = settingsFileOrNull.GetText(cancellationToken);

                try
                {
                    return ReadSourceText(fileText, reader =>
                    {
                        var serializer = new DataContractSerializer(typeof(AnalyzerSettings));
                        return (AnalyzerSettings)serializer.ReadObject(reader);
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.Write("Failed to parse analyzer settings file. Using default settings. Exception: " + ex);
                }
            }

            return AnalyzerSettings.Default;
        }

        internal static bool IsSettingsFile([NotNull] string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            return string.Equals(fileName, SettingsFileName, StringComparison.OrdinalIgnoreCase);
        }

        [NotNull]
        private static TResult ReadSourceText<TResult>([NotNull] SourceText sourceText,
            [NotNull] Func<XmlReader, TResult> readAction, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);

            sourceText.Write(writer, cancellationToken);
            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);

            using var textReader = new StreamReader(stream);
            using var xmlReader = XmlReader.Create(textReader);

            return readAction(xmlReader);
        }

        [NotNull]
        public static string ToFileContent([NotNull] AnalyzerSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));

            Encoding encoding = CreateEncoding();

            return GetStringForXml(encoding, writer =>
            {
                var serializer = new DataContractSerializer(typeof(AnalyzerSettings));
                serializer.WriteObject(writer, settings);
            });
        }

        [NotNull]
        private static string GetStringForXml([NotNull] Encoding encoding, [NotNull] Action<XmlWriter> writeAction)
        {
            using var stream = new MemoryStream();
            using var textWriter = new StreamWriter(stream);

            using var xmlWriter = XmlWriter.Create(textWriter, new XmlWriterSettings
            {
                Encoding = encoding,
                Indent = true
            });

            writeAction(xmlWriter);

            xmlWriter.Flush();
            textWriter.Flush();

            stream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
